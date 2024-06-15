using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes.V2;
using GVR.Core;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public class LabLockableHandler : MonoBehaviour, ILabHandler
    {
        public GameObject chestsGo;
        public GameObject doorsGo;

        public void Bootstrap()
        {
            var chestName = "CHESTBIG_OCCHESTLARGELOCKED.MDS";
            var mdh = ResourceLoader.TryGetModelHierarchy(chestName);
            var mdm = ResourceLoader.TryGetModelMesh(chestName);

            MeshFactory.CreateVob(chestName, mdm, mdh, Vector3.zero, Quaternion.identity, chestsGo, useTextureArray: false);


            var doorName = "DOOR_WOODEN";
            var mdlDoor = ResourceLoader.TryGetModel(doorName);

            MeshFactory.CreateVob(doorName, mdlDoor, Vector3.zero, Quaternion.identity, doorsGo, useTextureArray: false);
        }
    }
}
