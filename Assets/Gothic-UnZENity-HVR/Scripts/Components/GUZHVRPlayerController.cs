using HurricaneVR.Framework.Core.Player;
using MyBox;
using UnityEngine;

namespace GUZ.HVR.Components
{
    public class GUZHVRPlayerController : HVRPlayerController
    {
        private GUZHVRPlayerInputs _guzInputs => (GUZHVRPlayerInputs)Inputs;

        [Separator("GUZ - Settings")]
        public GameObject MainMenu;

        protected override void Update()
        {
            if (_guzInputs.IsMenuActivated)
            {
                // Toggle visibility
                MainMenu.SetActive(!MainMenu.activeSelf);
                Debug.Log("Menu activated");
            }
        }
    }
}
