using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core._Npc2
{
    /// <summary>
    /// Manage all NPC related calls a(Ext* engine calls and e.g. load Npcs at WorldSceneManager time)
    /// </summary>
    public class NpcManager2
    {
        // Supporter class where the whole Init() logic is outsourced for better readability.
        private NpcInitializer2 _initializer = new ();

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

        public void ExtMdlSetModelScale(NpcInstance npc, Vector3 scale)
        {
            var npcGo = npc.GetUserData2().Go;

            // FIXME - If fatness is applied before, we reset it here. We need to do proper Vector multiplication here.
            npcGo.transform.localScale = scale;
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
    }
}
