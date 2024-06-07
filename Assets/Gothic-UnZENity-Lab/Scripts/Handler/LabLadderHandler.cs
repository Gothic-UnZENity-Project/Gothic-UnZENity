using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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


            // Data taken from VobCreator.CreateLadder()
            var grabComp = vobObj.AddComponent<XRGrabInteractable>();
            var rigidbodyComp = vobObj.GetComponent<Rigidbody>();
            var meshColliderComp = vobObj.GetComponentInChildren<MeshCollider>();

            meshColliderComp.convex = true; // We need to set it to overcome Physics.ClosestPoint warnings.
            vobObj.tag = Constants.ClimbableTag;
            rigidbodyComp.isKinematic = true;
            grabComp.throwOnDetach = false; // Throws errors and isn't needed as we don't want to move the kinematic ladder when released.
            grabComp.trackPosition = false;
            grabComp.trackRotation = false;
            grabComp.selectMode = InteractableSelectMode.Multiple; // With this, we can grab with both hands!
        }
    }
}
