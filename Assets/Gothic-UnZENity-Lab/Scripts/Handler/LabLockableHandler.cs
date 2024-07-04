using GUZ.Core;
using GUZ.Core.Creator.Meshes.V2;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Lab.Handler
{
    public class LabLockableHandler : MonoBehaviour, ILabHandler
    {
        [FormerlySerializedAs("chestsGo")] public GameObject ChestsGo;
        [FormerlySerializedAs("doorsGo")] public GameObject DoorsGo;

        public void Bootstrap()
        {
            var chestPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobContainer);
            var chestName = "CHESTBIG_OCCHESTLARGELOCKED.MDS";
            var mdh = ResourceLoader.TryGetModelHierarchy(chestName);
            var mdm = ResourceLoader.TryGetModelMesh(chestName);

            MeshFactory.CreateVob(chestName, mdm, mdh, Vector3.zero, Quaternion.identity,
                rootGo: chestPrefab, parent: ChestsGo, useTextureArray: false);

            var doorPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobDoor);
            var doorName = "DOOR_WOODEN";
            var mdlDoor = ResourceLoader.TryGetModel(doorName);

            MeshFactory.CreateVob(doorName, mdlDoor, Vector3.zero, Quaternion.identity,
                rootGo: doorPrefab, parent: DoorsGo, useTextureArray: false);
        }
    }
}
