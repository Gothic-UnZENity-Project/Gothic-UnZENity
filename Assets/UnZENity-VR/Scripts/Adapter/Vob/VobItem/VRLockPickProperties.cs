#if GUZ_HVR_INSTALLED
using GUZ.VR.Adapter.Vob.VobDoor;
using UnityEngine;

namespace GUZ.VR.Adapter.Vob.VobItem
{
    public class VRLockPickProperties : VRVobItemProperties
    {
        public bool IsInsideLock;
        public VRDoorLockInteraction ActiveDoorLock;
        public Transform HoldingHand;
    }
}
#endif
