using GUZ.VR.Components.VobDoor;
using UnityEngine;

namespace GUZ.VR.Properties.VobItem
{
    public class VRVobLockPickProperties : VRVobItemProperties
    {
        public bool IsInsideLock;
        public VRDoorLockInteraction ActiveDoorLock;
        public Transform HoldingHand;
    }
}
