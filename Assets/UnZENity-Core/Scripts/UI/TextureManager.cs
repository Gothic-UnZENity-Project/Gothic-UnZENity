using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using UnityEngine;

public class TextureManager : MonoBehaviour
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
    public Material SliderMaterial;
    public Material SliderPositionMaterial;
    public Material ArrowMaterial;
    public Material FillerMaterial;
    
    // Loading
    public Material LoadingBarBackgroundMaterial;
    public Material LoadingBarMaterial;
    public Material LoadingSphereMaterial;

    // Misc
    public Material SkyMaterial;
    public Material MapMaterial;
    public Material BackgroundMaterial;

    
    private void Start()
    {
        MainMenuImageBackgroundMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        MainMenuBackgroundMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        MainMenuSaveLoadBackgroundMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        MainMenuTextImageMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Transparent);

        GothicLoadingMenuMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        LoadingBarBackgroundMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        LoadingBarMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);

        LoadingSphereMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        LoadingSphereMaterial.color = new Color(.25f, .25f, .25f, 1f); // dark gray
    }

    public void LoadLoadingDefaultTextures()
    {
        MainMenuImageBackgroundMaterial.mainTexture = TextureCache.TryGetTexture("STARTSCREEN.TGA");
        MainMenuBackgroundMaterial.mainTexture = TextureCache.TryGetTexture("MENU_INGAME.TGA");
        MainMenuSaveLoadBackgroundMaterial.mainTexture = TextureCache.TryGetTexture("MENU_SAVELOAD_BACK.TGA");
        MainMenuTextImageMaterial.mainTexture = TextureCache.TryGetTexture("MENU_GOTHIC.TGA");
        MenuChoiceBackMaterial.mainTexture = TextureCache.TryGetTexture("MENU_CHOICE_BACK.TGA");

        GothicLoadingMenuMaterial.mainTexture = TextureCache.TryGetTexture("LOADING.TGA");
        LoadingBarBackgroundMaterial.mainTexture = TextureCache.TryGetTexture("PROGRESS.TGA");
        LoadingBarMaterial.mainTexture = TextureCache.TryGetTexture("PROGRESS_BAR.TGA");
        BackgroundMaterial.mainTexture = TextureCache.TryGetTexture("LOG_PAPER.TGA");
        ButtonMaterial.mainTexture = TextureCache.TryGetTexture("INV_SLOT.TGA");
        SliderMaterial.mainTexture = TextureCache.TryGetTexture("MENU_SLIDER_BACK.TGA");
        SliderPositionMaterial.mainTexture = TextureCache.TryGetTexture("MENU_SLIDER_POS.TGA");
        FillerMaterial.mainTexture = TextureCache.TryGetTexture("MENU_BUTTONBACK.TGA");
        ArrowMaterial.mainTexture = TextureCache.TryGetTexture("U.TGA");
        MapMaterial.mainTexture = TextureCache.TryGetTexture("MAP_WORLD_ORC.TGA");
    }

    public void SetTexture(string texture, Material material)
    {
        material.mainTexture = TextureCache.TryGetTexture(texture);
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
}
