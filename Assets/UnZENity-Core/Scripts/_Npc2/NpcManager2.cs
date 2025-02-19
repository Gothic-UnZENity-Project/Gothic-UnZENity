using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Vm;
using MyBox;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using Vector3 = System.Numerics.Vector3;

namespace GUZ.Core._Npc2
{
    /// <summary>
    /// Manage all NPC related calls a(Ext* engine calls and e.g. load Npcs at WorldSceneManager time)
    /// </summary>
    public class NpcManager2
    {
        // Supporter class where the whole Init() logic is outsourced for better readability.
        private NpcInitializer2 _initializer = new ();
        private static DaedalusVm _vm => GameData.GothicVm;

        public async Task CreateWorldNpcs(LoadingManager loading)
        {
            await _initializer.InitNpcsNewGame(loading);
        }

        public void ExtWldInsertNpc(int npcInstanceIndex, string spawnPoint)
        {
            _initializer.ExtWldInsertNpc(npcInstanceIndex, spawnPoint);
        }

        public void ExtNpcSetTalentValue(NpcInstance npc, VmGothicEnums.Talent talent, int level)
        {
            var props = npc.GetUserData2().Properties;
            props.Talents[talent] = level;
        }

        public void ExtMdlSetVisual(NpcInstance npc, string visual)
        {
            var props = npc.GetUserData2().Properties;
            props.MdsNameBase = visual;
        }

        public void ExtSetVisualBody(VmGothicExternals.ExtSetVisualBodyData data)
        {
            var props = data.Npc.GetUserData2().Properties;

            props.BodyData = data;

            if (data.Armor >= 0)
            {
                var armorData = VmInstanceManager.TryGetItemData(data.Armor);
                props.EquippedItems.Add(VmInstanceManager.TryGetItemData(data.Armor));
                props.MdmName = armorData.VisualChange;
            }
            else
            {
                props.MdmName = data.Body;
            }
        }

        public NpcInstance ExtHlpGetNpc(int instanceId)
        {
            return MultiTypeCache.NpcCache2
                .FirstOrDefault(i => i.Instance.Index == instanceId)?
                .Instance;
        }

        public void ExtNpcChangeAttribute(NpcInstance npc, int attributeId, int value)
        {
            var vob = npc.GetUserData2().Vob;

            vob.Attributes[attributeId] = value;
        }

        public void ExtCreateInvItems(NpcInstance npc, uint itemId, int amount)
        {
            // We also initialize NPCs inside Daedalus when we load a save game. It's needed as some data isn't stored on save games.
            // But e.g. inventory items will be skipped as they are stored inside save game VOBs.
            if (!GameGlobals.SaveGame.IsWorldLoadedForTheFirstTime)
            {
                return;
            }

            var props = npc.GetUserData2().Properties;
            if (props == null)
            {
                Debug.LogError($"NPC not found with index {npc.Index}");
                return;
            }
            props.Items.TryAdd(itemId, amount);
            props.Items[itemId] += amount;
        }

        /// <summary>
        /// We need to first Alloc() hero data space and put the instance to the cache.
        /// Then we initialize it. (During Init, PC_HERO:Npc_Default->Prototype:Npc_Default will call SetTalentValue where we need the lookup to fetch the NpcInstance).
        ///
        /// This method will get called every time we spawn into another world. We therefore need to check if initialize the first time or we only need to set the lookup cache.
        /// </summary>
        public void CacheHero()
        {
            if (GameData.GothicVm.GlobalHero != null)
            {
                // We assume, that this call is only made when the cache got cleared before as we loaded another world.
                // Therefore, we re-add it now.
                MultiTypeCache.NpcCache.Add(((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData());

                return;
            }


            // Initial setup
            var playerGo = GameObject.FindWithTag(Constants.PlayerTag);

            // Flat player
            if (playerGo == null)
            {
                playerGo = GameObject.FindWithTag(Constants.MainCameraTag);
            }

            var heroInstance = GameData.GothicVm.AllocInstance<NpcInstance>(GameGlobals.Config.Gothic.PlayerInstanceName);

            var vobNpc = new ZenKit.Vobs.Npc();
            vobNpc.Name = GameGlobals.Config.Gothic.PlayerInstanceName;
            vobNpc.Player = true;

            var npcData = new NpcContainer2
            {
                Instance = heroInstance,
                Vob = vobNpc,
                Properties = new()
            };

            npcData.Properties.Head = Camera.main!.transform;

            heroInstance.UserData = npcData;

            MultiTypeCache.NpcCache2.Add(npcData);
            _vm.InitInstance(heroInstance);

            _vm.GlobalHero = heroInstance;
        }

        public void ExtMdlSetModelScale(NpcInstance npc, Vector3 scale)
        {
            // FIXME - Set this value on actual GameObject later.
            npc.GetUserData2().Vob.ModelScale = scale;
        }

        public void ExtSetModelFatness(NpcInstance npc, float fatness)
        {
            // FIXME - Set this value on actual GameObject later.
            npc.GetUserData2().Vob.ModelFatness = fatness;
        }

        public void ExtEquipItem(NpcInstance npc, int itemId)
        {
            var props = npc.GetUserData2().Properties;
            var itemData = VmInstanceManager.TryGetItemData(itemId);

            props.EquippedItems.Add(itemData);
        }

        public void ExtApplyOverlayMds(NpcInstance npc, string overlayName)
        {
            npc.GetUserData2().Properties.MdsOverlayName = overlayName;
        }

        public void ExtNpcSetToFistMode(NpcInstance npc)
        {
            var npcProperties = npc.GetUserData2().Properties;

            npcProperties.WeaponState = VmGothicEnums.WeaponState.Fist;

            // if npc has item in hand remove it and set weapon to fist
            // Some animations need to force remove items, some not.
            if (npcProperties.UsedItemSlot.IsNullOrEmpty())
            {
                return;
            }

            var slotGo = npc.GetUserData2().Go.FindChildRecursively(npcProperties.UsedItemSlot);
            var item = slotGo!.transform.GetChild(0);

            Object.Destroy(item.gameObject);
        }
    }
}
