#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Globals;
using HurricaneVR.Framework.ControllerInput;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.VR.UI.Menus
{
    public class TutorialHandler : MonoBehaviour
    {
        [SerializeField]
        private Image[] _backgroundImages;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void Start()
        {
            var backPic = GameGlobals.Textures.GetMaterial(Constants.DaedalusMenu.BackPic);
            
            foreach (var image in _backgroundImages)
            {
                image.material = backPic;
            }
        }
    }
}
#endif
