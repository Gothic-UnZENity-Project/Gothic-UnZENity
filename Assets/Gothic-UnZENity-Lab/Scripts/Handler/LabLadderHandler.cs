using GUZ.Core.Context;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public class LabLadderLabHandler : MonoBehaviour, ILabHandler
    {
        public GameObject ladderSlot;

        public void Bootstrap()
        {
            var itemPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobLadder);
            var ladderName = "LADDER_3.MDL";
            var mdl = ResourceLoader.TryGetModel(ladderName);

            var vobObj = MeshFactory.CreateVob(ladderName, mdl, Vector3.zero, Quaternion.Euler(0, 270, 0),
                ladderSlot, rootGo: itemPrefab, useTextureArray: false);

            GameObject climbableObj = vobObj.GetComponentInChildren<MeshCollider>().gameObject;
            GUZContext.InteractionAdapter.AddClimbingComponent(climbableObj);
        }
    }
}
