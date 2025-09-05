using GUZ.Core;
using GUZ.Core.Services;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public class LabLadderLabHandler : AbstractLabHandler
    {
        public GameObject LadderSlot;

        public override void Bootstrap()
        {
            var itemPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobLadder);
            var ladderName = "LADDER_3.MDL";
            var mdl = ResourceLoader.TryGetModel(ladderName);

            MeshService.CreateVob(ladderName, mdl, Vector3.zero, Quaternion.Euler(0, 180, 0), LadderSlot, itemPrefab, false);
        }
    }
}
