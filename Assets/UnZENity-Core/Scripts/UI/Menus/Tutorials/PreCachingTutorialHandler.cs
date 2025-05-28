using System;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.Core.UI.Menus
{
    public class PreCachingTutorialHandler : MonoBehaviour
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
