using GUZ.Core;
using GUZ.Core.Context;
using GUZ.Core.Creator.Meshes.V2;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Lab.Handler
{
    public class LabLadderLabHandler : MonoBehaviour, ILabHandler
    {
        [FormerlySerializedAs("ladderSlot")] public GameObject LadderSlot;

        public void Bootstrap()
        {
            var itemPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobLadder);
            var ladderName = "LADDER_3.MDL";
            var mdl = ResourceLoader.TryGetModel(ladderName);

            var vobObj = MeshFactory.CreateVob(ladderName, mdl, Vector3.zero, Quaternion.Euler(0, 270, 0),
                LadderSlot, itemPrefab, false);

            var climbableObj = vobObj.GetComponentInChildren<MeshCollider>().gameObject;
            GuzContext.InteractionAdapter.AddClimbingComponent(climbableObj);
        }
    }
}
