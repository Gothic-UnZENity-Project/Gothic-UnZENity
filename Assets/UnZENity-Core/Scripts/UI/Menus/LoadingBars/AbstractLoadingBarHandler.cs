using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.Core.UI.Menus.LoadingBars
{
    public abstract class AbstractLoadingBarHandler : MonoBehaviour
    {
        public Image LoadingImage;
        public Image ProgressBackgroundImage;
        public Image ProgressBarImage;


        public abstract List<string> GetProgressTypes();
        
        protected virtual void Start()
        {
            SetMaterials();
        }
        
        protected virtual void SetMaterials()
        {
            var tm = GameGlobals.Textures;
            LoadingImage.material = tm.GothicLoadingMenuMaterial;
            ProgressBackgroundImage.material = tm.LoadingBarBackgroundMaterial;
            ProgressBarImage.material = tm.LoadingBarMaterial;
        }
    }
}
