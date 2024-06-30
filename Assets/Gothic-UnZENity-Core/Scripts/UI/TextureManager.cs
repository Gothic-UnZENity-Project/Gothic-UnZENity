using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.Serialization;

public class TextureManager : MonoBehaviour
{
    [FormerlySerializedAs("mainMenuImageBackgroundMaterial")]
    public Material MainMenuImageBackgroundMaterial;

    [FormerlySerializedAs("mainMenuBackgroundMaterial")]
    public Material MainMenuBackgroundMaterial;

    [FormerlySerializedAs("mainMenuSaveLoadBackgroundMaterial")]
    public Material MainMenuSaveLoadBackgroundMaterial;

    [FormerlySerializedAs("mainMenuTextImageMaterial")]
    public Material MainMenuTextImageMaterial;

    [FormerlySerializedAs("backgroundMaterial")]
    public Material BackgroundMaterial;

    [FormerlySerializedAs("buttonMaterial")]
    public Material ButtonMaterial;

    [FormerlySerializedAs("sliderMaterial")]
    public Material SliderMaterial;

    [FormerlySerializedAs("sliderPositionMaterial")]
    public Material SliderPositionMaterial;

    [FormerlySerializedAs("arrowMaterial")]
    public Material ArrowMaterial;

    [FormerlySerializedAs("fillerMaterial")]
    public Material FillerMaterial;

    [FormerlySerializedAs("skyMaterial")] public Material SkyMaterial;
    [FormerlySerializedAs("mapMaterial")] public Material MapMaterial;

    [FormerlySerializedAs("gothicLoadingMenuMaterial")]
    public Material GothicLoadingMenuMaterial;

    [FormerlySerializedAs("loadingBarBackgroundMaterial")]
    public Material LoadingBarBackgroundMaterial;

    [FormerlySerializedAs("loadingBarMaterial")]
    public Material LoadingBarMaterial;

    [FormerlySerializedAs("loadingSphereMaterial")]
    public Material LoadingSphereMaterial;

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
