using System;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using UnityEngine;

public class TextureManager : MonoBehaviour
{
    // (Main) Menu
    [NonSerialized]
    public Material MainMenuImageBackgroundMaterial;
    [NonSerialized]
    public Material MainMenuBackgroundMaterial;
    [NonSerialized]
    public Material MainMenuSaveLoadBackgroundMaterial;
    [NonSerialized]
    public Material MainMenuTextImageMaterial;
    public Material MenuChoiceBackMaterial;

    // Menu
    [NonSerialized]
    public Material GothicLoadingMenuMaterial;
    public Material ButtonMaterial;
    public Material SliderMaterial;
    public Material SliderPositionMaterial;
    public Material FillerMaterial;
    [NonSerialized] public Material ArrowUpMaterial;
    [NonSerialized] public Material ArrowDownMaterial;
    [NonSerialized] public Material ArrowLeftMaterial;

    // Loading
    [NonSerialized]
    public Material LoadingBarBackgroundMaterial;
    [NonSerialized]
    public Material LoadingBarMaterial;

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

        // Menu
        ArrowUpMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        ArrowDownMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        ArrowLeftMaterial = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
    }

    public void Init()
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
        MapMaterial.mainTexture = TextureCache.TryGetTexture("MAP_WORLD_ORC.TGA");

        // Menu
        ArrowUpMaterial.mainTexture = TextureCache.TryGetTexture("O.TGA");
        ArrowDownMaterial.mainTexture = TextureCache.TryGetTexture("U.TGA");
        ArrowLeftMaterial.mainTexture = TextureCache.TryGetTexture("L.TGA");
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

    /// <summary>
    /// Create a new material and assign texture to it.
    /// </summary>
    public Material GetMaterial(string textureName)
    {
        var material = GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        material.mainTexture = TextureCache.TryGetTexture(textureName);

        return material;
    }
}
