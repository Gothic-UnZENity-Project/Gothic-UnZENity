using HurricaneVR.Framework.Components;
using MyBox;
using UnityEngine;

namespace GUZ.HVR.Components.VobContainer
{
    public class HVRVobContainerPhysicsDoor : HVRPhysicsDoor
    {
        [Separator("GUZ - Settings")]
        [SerializeField] private GameObject _inventory;
        
        // Flags to ensure we always call On*() once, everytime open/close status changes.
        private bool _onOpenedHandled;
        private bool _onClosedHandled;

        public override void Start()
        {
            base.Start();
            
            _inventory.SetActive(false);
        }
        
        protected override void Update()
        {
            base.Update();

            if (Opened && !_onOpenedHandled)
            {
                OnOpened();
            }

            if (Closed && !_onClosedHandled)
            {
                OnClosed();
            }
        }
        
        private void OnOpened()
        {
            // Render items if opened for the first time.
            _inventory.SetActive(true);
            
            _onOpenedHandled = true;
            _onClosedHandled = false;
        }
        
        private void OnClosed()
        {
            _inventory.SetActive(false);
            
            _onOpenedHandled = false;
            _onClosedHandled = true;
        }
    }
}
