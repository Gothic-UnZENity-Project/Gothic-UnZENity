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
        public GameObject WheelsGO;
        public GameObject InteractablesGO;

        public void Bootstrap()
        {
            InitOCMobContainer();
            InitOCMobDoor();
            InitOCMobFire();
            InitOCMobBed();
            InitOCMobSwitch();
            InitOCMobWheel();
            InitOCMobInter();
        }

        private void InitOCMobContainer()
        {
            var chestPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobContainer);
            var chestName = "CHESTBIG_OCCHESTLARGELOCKED.MDS";
            var mdh = ResourceLoader.TryGetModelHierarchy(chestName);
            var mdm = ResourceLoader.TryGetModelMesh(chestName);

            MeshFactory.CreateVob(chestName, mdm, mdh, Vector3.zero, Quaternion.identity,
                rootGo: chestPrefab, parent: ContainersGO, useTextureArray: false);
        }

        private void InitOCMobDoor()
        {
            var doorPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobDoor);
            var doorName = "DOOR_WOODEN";
            var mdlDoor = ResourceLoader.TryGetModel(doorName);

            MeshFactory.CreateVob(doorName, mdlDoor, new Vector3(0, 0.2f, 0), Quaternion.identity,
                rootGo: doorPrefab, parent: DoorsGO, useTextureArray: false);


            // Yes, that's correct. Beds are mostly of type oCMobDoor inside G1. ;-)
            // TODO BED_1_OC.ASC
            var doorPrefab2 = ResourceLoader.TryGetPrefabObject(PrefabType.VobDoor);
            var door2Name = "BED_1_OC";
            var mdlDoor2 = ResourceLoader.TryGetModel(door2Name);

            MeshFactory.CreateVob(door2Name, mdlDoor2, new Vector3(0, 0, -3), Quaternion.identity,
                rootGo: doorPrefab2, parent: DoorsGO, useTextureArray: false);
        }

        private void InitOCMobFire()
        {
            // TODO FIREPLACE_HIGH.ASC
        }

        private void InitOCMobBed()
        {
            // TODO BEDLOW_PSI.ASC
        }

        private void InitOCMobSwitch()
        {
            // TODO LEVER_1_OC.MDS
            // TODO TOUCHPLATE_STONE.MDS
            // TODO VWHEEL_1_OC.MDS
        }

        private void InitOCMobWheel()
        {
            // TODO THRONE_BIG.ASC
            // TODO BABEBED_1.ASC
        }

        private void InitOCMobInter()
        {
            // TODO WHEEL_1_OC.mds
            // TODO SMOKE_WATERPIPE.mds
            // TODO CHAIR_1_OC.ASC
        }
    }
}
