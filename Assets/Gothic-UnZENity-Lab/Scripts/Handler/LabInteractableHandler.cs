using GUZ.Core;
using GUZ.Core.Creator.Meshes.V2;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public class LabInteractableHandler : MonoBehaviour, ILabHandler
    {
        public GameObject ContainersGO;
        public GameObject DoorsGO;
        public GameObject FiresGO;
        public GameObject BedsGO;
        public GameObject SwitchesGO;
        public GameObject InteractablesGO;
        public GameObject WheelsGO;

        public void Bootstrap()
        {
            InitOCMobDoor();
            InitOCMobContainer();
            InitOCMobFire();
            InitOCMobBed();
            InitOCMobSwitch();
            InitOCMobInter();
            InitOCMobWheel();
        }


        private void InitOCMobDoor()
        {
            SpawnInteractable("DOOR_WOODEN", PrefabType.VobDoor, DoorsGO, new Vector3(0, 0.2f, 0));

            // Yes, that's correct. Beds are mostly of type oCMobDoor inside G1. ;-)
            SpawnInteractable("BED_1_OC", PrefabType.VobDoor, DoorsGO, new Vector3(0, 0, -4));
        }

        private void InitOCMobContainer()
        {
            SpawnInteractable("CHESTSMALL_OCCHESTSMALLLOCKED", PrefabType.VobContainer, ContainersGO, rotation: Quaternion.Euler(0, 0, 0));
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

        private void SpawnInteractable(string mdlName, PrefabType type, GameObject parentGo, Vector3 position = default, Quaternion rotation = default)
        {
            var prefab = ResourceLoader.TryGetPrefabObject(type);
            var mdl = ResourceLoader.TryGetModel(mdlName);

            if (mdl == null)
            {
                Debug.LogError("LabInteractableHandler: Element has no .mdl file: " + mdlName);
                return;
            }

            MeshFactory.CreateVob(mdlName, mdl, position, rotation,
                rootGo: prefab, parent: parentGo, useTextureArray: false);
        }
    }
}
