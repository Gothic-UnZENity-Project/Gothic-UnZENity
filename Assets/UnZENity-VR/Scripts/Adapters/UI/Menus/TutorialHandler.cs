#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Const;
using GUZ.Core.Services.Meshes;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.VR.Adapters.UI.Menus
{
    public class TutorialHandler : MonoBehaviour
    {
        [SerializeField] private Image[] _backgroundImages;

        
        [Inject] private readonly TextureService _textureService;

        
        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void Start()
        {
            var backPic = _textureService.GetMaterial(Constants.DaedalusMenu.BackPic);
            
            foreach (var image in _backgroundImages)
            {
                image.material = backPic;
            }
        }
    }
}
#endif
