using GUZ.VR.Properties.VobItem;
using UnityEngine;

namespace GUZ.VR.Components.VobItem
{
    public class VRLockPickInteraction : MonoBehaviour
    {
        [SerializeField] private VRVobLockPickProperties _properties;

        private void Update()
        {
            if (!_properties.IsInsideLock)
            {
                return;
            }


        }

        private void CalculateRotation()
        {
            /**
             * 0. Set start rotation of hand(s) which grab item. (As we have 2 hands, we need to check via array[2] for both items always!
             * 1.a If rotation is ~45° right - trigger right-information to currently active door
             * 1.b ~45° left - same
             */
        }
    }
}
