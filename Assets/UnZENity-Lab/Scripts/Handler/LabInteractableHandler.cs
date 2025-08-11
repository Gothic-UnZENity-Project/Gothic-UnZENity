using System.Collections;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Lab.Handler
{
    public class LabInteractableHandler : AbstractLabHandler
    {
        public GameObject Weapons1HGO;
        public GameObject ContainersGO;
        public GameObject DoorsGO;
        public GameObject FiresGO;
        public GameObject BedsGO;
        public GameObject SwitchesGO;
        public GameObject InteractablesGO;
        public GameObject WheelsGO;

        public override void Bootstrap()
        {
            InitWeapons1H();
            InitWeapons2H();
            // FIXME - Need to initialize them via VobLoader.LoadNow(IVob) instead of loading mesh. Otherwise we get exceptions in child Start() calls.
            // InitOCMobDoor();
            // StartCoroutine(InitOCMobContainer());
            // InitOCMobFire();
            // InitOCMobBed();
            // InitOCMobSwitch();
            // InitOCMobInter();
            // InitOCMobWheel();
        }

        private void InitWeapons1H()
        {
            var itemNames = GameData.GothicVm.GetInstanceSymbols("C_Item").Select(i => i.Name).ToList();

            // var itemNames = new []{
            //     "ITMW_1H_CLUB_01",
            //     "ITMW_1H_SLEDGEHAMMER_01",
            //     "ITMW_1H_SWORD_SHORT_05"
            // };
            
            var items = itemNames.ToDictionary(itemName => itemName, VmInstanceManager.TryGetItemData)
                    .Where(i => i.Value.MainFlag == (int)VmGothicEnums.ItemFlags.ItemKatNf)
                    .Where(i => (i.Value.Flags & ((int)VmGothicEnums.ItemFlags.ItemSwd | (int)VmGothicEnums.ItemFlags.ItemAxe)) != 0);
            
            var zPosition = 0f;
            foreach (var item in items)
            {
                var vobContainer = GameGlobals.Vobs.CreateItem(new Item()
                {
                    Name = item.Key,
                    Position = new Vector3(0f, 1f, zPosition).ToZkVector(),
                    Rotation = Quaternion.Euler(new Vector3(0, 0, -90)).ToZkMatrix(), // Quaternion.identity.ToZkMatrix(), 
                    Visual = new VisualMesh(),
                    Instance = item.Key
                });

                vobContainer.Go.SetParent(Weapons1HGO);
                zPosition -= 0.5f;
            }
        }
        
        private void InitWeapons2H()
        {
            var itemNames = GameData.GothicVm.GetInstanceSymbols("C_Item").Select(i => i.Name).ToList();

            // var itemNames = new []{
            //     "ITMW_1H_CLUB_01",
            //     "ITMW_1H_SLEDGEHAMMER_01",
            //     "ITMW_1H_SWORD_SHORT_05"
            // };
            
            var items = itemNames.ToDictionary(itemName => itemName, VmInstanceManager.TryGetItemData)
                .Where(i => i.Value.MainFlag == (int)VmGothicEnums.ItemFlags.ItemKatNf)
                .Where(i => (i.Value.Flags & ((int)VmGothicEnums.ItemFlags.Item2HdSwd | (int)VmGothicEnums.ItemFlags.Item2HdAxe)) != 0);
            
            var zPosition = 0f;
            foreach (var item in items)
            {
                var vobContainer = GameGlobals.Vobs.CreateItem(new Item()
                {
                    Name = item.Key,
                    Position = new Vector3(2f, 1f, zPosition).ToZkVector(),
                    Rotation = Quaternion.Euler(new Vector3(0, 0, -90)).ToZkMatrix(), // Quaternion.identity.ToZkMatrix(), 
                    Visual = new VisualMesh(),
                    Instance = item.Key
                });

                vobContainer.Go.SetParent(Weapons1HGO);
                zPosition -= 0.5f;
            }
        }

        private void InitOCMobDoor()
        {
            SpawnInteractable("DOOR_WOODEN", PrefabType.VobDoor, DoorsGO, new Vector3(0, 0.2f, 0));

            // Yes, that's correct. Beds are mostly of type oCMobDoor inside G1. ;-)
            SpawnInteractable("BED_1_OC", PrefabType.VobDoor, DoorsGO, new Vector3(0, 0, -4));
        }

        private IEnumerator InitOCMobContainer()
        {
            var chest1 = SpawnInteractable("CHESTBIG_OCCHESTLARGE", PrefabType.VobContainer, ContainersGO, position: new(0,0,0));
            SpawnInteractable("CHESTBIG_OCCHESTMEDIUM", PrefabType.VobContainer, ContainersGO, position: new(0,0,-2));
            SpawnInteractable("CHESTBIG_OCCRATELARGE", PrefabType.VobContainer, ContainersGO, position: new(0,0,-4));
            SpawnInteractable("CHESTBIG_ORCMUMMY", PrefabType.VobContainer, ContainersGO, position: new(0,0,-6));
            SpawnInteractable("CHESTSMALL_OCCHESTSMALL", PrefabType.VobContainer, ContainersGO, position: new(0,0,-8));
            SpawnInteractable("CHESTSMALL_OCCHESTSMALLLOCKED", PrefabType.VobContainer, ContainersGO, position: new(0,0,-10));
            SpawnInteractable("CHESTSMALL_OCCRATESMALL", PrefabType.VobContainer, ContainersGO, position: new(0,0,0-12));
            SpawnInteractable("CHESTSMALL_OCCRATESMALLLOCKED", PrefabType.VobContainer, ContainersGO, position: new(0,0,-14));
            
            var item1 = SpawnItem("ItMwPickaxe", ContainersGO, new Vector3(1.25f, 0.25f, 0));

            // Wait 1 frame for Sockets to become active.
            yield return null;
        }

        private void InitOCMobFire()
        {
            SpawnInteractable("FIREPLACE_HIGH", PrefabType.VobFire, FiresGO);
        }

        private void InitOCMobBed()
        {
            SpawnInteractable("BEDLOW_PSI", PrefabType.VobBed, BedsGO, new Vector3(0, 0.1f, -1), Quaternion.Euler(0, 90, 0));
        }

        private void InitOCMobSwitch()
        {
            SpawnInteractable("LEVER_1_OC", PrefabType.VobSwitch, SwitchesGO, new Vector3(0, 1, 0), Quaternion.Euler(0, 180, 0));
            SpawnInteractable("TOUCHPLATE_STONE", PrefabType.VobSwitch, SwitchesGO, new Vector3(0,0, -3), Quaternion.Euler(0, 180, 0));
            SpawnInteractable("VWHEEL_1_OC", PrefabType.VobSwitch, SwitchesGO, new Vector3(0,0, -6), Quaternion.Euler(0, 90, 0));
        }

        private void InitOCMobInter()
        {
            SpawnInteractable("SMOKE_WATERPIPE", PrefabType.VobInteractable, InteractablesGO);
            SpawnInteractable("CHAIR_1_OC", PrefabType.VobInteractable, InteractablesGO, new Vector3(0,0, -3));
        }

        private void InitOCMobWheel()
        {
            SpawnInteractable("THRONE_BIG", PrefabType.VobWheel, WheelsGO, rotation: Quaternion.Euler(0, 180, 0));
            SpawnInteractable("BABEBED_1", PrefabType.VobWheel, WheelsGO, new Vector3(0, 0, -4));
        }
    }
}
