#if GUZ_HVR_INSTALLED
using GUZ.VR.Adapters.Vob.VobDoor;
using UnityEngine;

namespace GUZ.VR.Adapters.Vob.VobItem
{
    public class VRLockPickProperties : VRVobItemProperties
    {
        public bool IsInsideLock;
        public VRDoorLockInteraction ActiveDoorLock;
        public Transform HoldingHand;
    }
}
#endif
