using GUZ.Core.Const;
using GUZ.Core.Extensions;
using GUZ.Core.Services.Caches;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.Core.Services.Meshes
{
    public class TextureService
    {
        // (Main) Menu
        public Material MainMenuImageBackgroundMaterial;
        public Material MainMenuBackgroundMaterial;
        public Material MainMenuSaveLoadBackgroundMaterial;
        public Material MainMenuTextImageMaterial;
        public Material MenuChoiceBackMaterial;

        // Menu
        public Material GothicLoadingMenuMaterial;
        public Material ButtonMaterial;
        public Material FillerMaterial;
        public Material ArrowUpMaterial;
        public Material ArrowDownMaterial;
        public Material ArrowLeftMaterial;

        // Loading
        public Material LoadingBarBackgroundMaterial;
        public Material LoadingBarMaterial;

        // Misc
        public Material SkyMaterial;
        public Material BackgroundMaterial;


        [Inject] private readonly TextureCacheService _textureCacheService;


        public void Init()
        {
            MainMenuImageBackgroundMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
            MainMenuBackgroundMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
            MainMenuSaveLoadBackgroundMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
            MainMenuTextImageMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Transparent);

            GothicLoadingMenuMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
            LoadingBarBackgroundMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
            LoadingBarMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);

            // Menu
            ArrowUpMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
            ArrowDownMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
            ArrowLeftMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);

        
            MainMenuImageBackgroundMaterial.mainTexture = _textureCacheService.TryGetTexture("STARTSCREEN.TGA");
            MainMenuBackgroundMaterial.mainTexture = _textureCacheService.TryGetTexture("MENU_INGAME.TGA");
            MainMenuSaveLoadBackgroundMaterial.mainTexture = _textureCacheService.TryGetTexture("MENU_SAVELOAD_BACK.TGA");
            MainMenuTextImageMaterial.mainTexture = _textureCacheService.TryGetTexture("MENU_GOTHIC.TGA");
            MenuChoiceBackMaterial.mainTexture = _textureCacheService.TryGetTexture("MENU_CHOICE_BACK.TGA");

            GothicLoadingMenuMaterial.mainTexture = _textureCacheService.TryGetTexture("LOADING.TGA");
            LoadingBarBackgroundMaterial.mainTexture = _textureCacheService.TryGetTexture("PROGRESS.TGA");
            LoadingBarMaterial.mainTexture = _textureCacheService.TryGetTexture("PROGRESS_BAR.TGA");
            BackgroundMaterial.mainTexture = _textureCacheService.TryGetTexture("LOG_PAPER.TGA");
            ButtonMaterial.mainTexture = _textureCacheService.TryGetTexture("INV_SLOT.TGA");
            FillerMaterial.mainTexture = _textureCacheService.TryGetTexture("MENU_BUTTONBACK.TGA");

            // Menu
            ArrowUpMaterial.mainTexture = _textureCacheService.TryGetTexture("O.TGA");
            ArrowDownMaterial.mainTexture = _textureCacheService.TryGetTexture("U.TGA");
            ArrowLeftMaterial.mainTexture = _textureCacheService.TryGetTexture("L.TGA");
        }

        public void SetTexture(string texture, Material material)
        {
            material.mainTexture = _textureCacheService.TryGetTexture(texture);
        }

        public Material GetEmptyMaterial(MaterialExtension.BlendMode blendMode)
        {
            var standardShader = Constants.ShaderUnlit;
            var material = new Material(standardShader);

            switch (blendMode)
            {
                case MaterialExtension.BlendMode.Opaque:
                    material.ToOpaqueMode();
                    break;
                case MaterialExtension.BlendMode.Transparent:
                    material.ToTransparentMode();
                    break;
            }

            // Enable clipping of alpha values.
            material.EnableKeyword("_ALPHATEST_ON");

            return material;
        }

        /// <summary>
        /// Create a new material and assign texture to it.
        /// </summary>
        public Material GetMaterial(string textureName, MaterialExtension.BlendMode blendMode = MaterialExtension.BlendMode.Opaque)
        {
            var material = GetEmptyMaterial(blendMode);
            material.mainTexture = _textureCacheService.TryGetTexture(textureName);

            return material;
        }
    }
}
