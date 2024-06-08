using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Creator.Meshes.V2;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public class LabLadderLabHandler : MonoBehaviour, ILabHandler
    {
        public GameObject ladderSlot;

        public void Bootstrap()
        {
            var itemPrefab = PrefabCache.TryGetObject(PrefabCache.PrefabType.VobLadder);
            var ladderName = "LADDER_3.MDL";
            var mdl = AssetCache.TryGetMdl(ladderName);

            var vobObj = MeshFactory.CreateVob(ladderName, mdl, Vector3.zero, Quaternion.Euler(0, 270, 0),
                ladderSlot, rootGo: itemPrefab, useTextureArray: false);

            GameObject climbableObj = vobObj.GetComponentInChildren<MeshCollider>().gameObject;
            GUZContext.InteractionAdapter.AddClimbingComponent(climbableObj);
        }
    }
}
