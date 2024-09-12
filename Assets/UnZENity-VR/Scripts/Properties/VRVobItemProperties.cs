using System;
using GUZ.Core.Properties;

namespace GUZ.VR.Properties
{
    public class VRVobItemProperties : VobItemProperties
    {
        public bool IsSocketed;
        public GrabStatus CurrentGrabStatus = GrabStatus.None;

        [Flags]
        public enum GrabStatus
        {
            None,
            LeftHand,
            RightHand
        }
    }
}
