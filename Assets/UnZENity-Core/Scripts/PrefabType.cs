using System;

namespace GUZ.Core
{
    public enum PrefabType
    {
        UiEmpty, // Contains a RectTransform only
        UiText,
        UiButton,
        UiButtonTextured,
        UiTexture,
        UiThumbnail,

        WayPoint,
        Vob,
        Npc,
        VobAnimate,
        VobContainer,
        VobDoor,
        VobFire,
        VobBed,
        VobWheel,
        VobSwitch,
        VobInteractable,
        VobInteractableSeat,
        VobMovable,
        VobSpot,
        VobPfx,
        VobMusic,
        VobSound,
        VobSoundDaytime,
        VobLadder,
        VobLight,

        VobItem,
        VobItemLockPick,
        
        Player,
        MainMenu,
        XRDeviceSimulator,
        StoryIntroduceChapter
    }

    public static class PrefabTypeExtension
    {
        public static string Path(this PrefabType type)
        {
            return type switch
            {
                PrefabType.UiEmpty => "Prefabs/UI/Empty",
                PrefabType.UiText => "Prefabs/UI/Text",
                PrefabType.UiButton => "Prefabs/UI/Button",
                PrefabType.UiButtonTextured => "Prefabs/UI/ButtonTextured",
                PrefabType.UiTexture => "Prefabs/UI/Texture",
                PrefabType.UiThumbnail => "Prefabs/UI/Thumbnail",

                PrefabType.WayPoint => "Prefabs/WayPoint",
                PrefabType.Vob => "Prefabs/Vobs/Vob",
                PrefabType.Npc => "Prefabs/Vobs/oCNpc",
                PrefabType.VobAnimate => "Prefabs/Vobs/zCVobAnimate",
                PrefabType.VobContainer => "Prefabs/Vobs/oCMobContainer",
                PrefabType.VobDoor => "Prefabs/Vobs/oCMobDoor",
                PrefabType.VobFire => "Prefabs/Vobs/oCMobFire",
                PrefabType.VobBed => "Prefabs/Vobs/oCMobBed",
                PrefabType.VobWheel => "Prefabs/Vobs/oCMobWheel",
                PrefabType.VobSwitch => "Prefabs/Vobs/oCMobSwitch",
                PrefabType.VobInteractable => "Prefabs/Vobs/oCMobInter",
                PrefabType.VobInteractableSeat => "Prefabs/Vobs/oCMobInterSeat",
                PrefabType.VobMovable => "Prefabs/Vobs/oCMobMovable",
                PrefabType.VobSpot => "Prefabs/Vobs/zCVobSpot",
                PrefabType.VobPfx => "Prefabs/Vobs/vobPfx",
                PrefabType.VobMusic => "Prefabs/Vobs/oCZoneMusic",
                PrefabType.VobSound => "Prefabs/Vobs/zCVobSound",
                PrefabType.VobSoundDaytime => "Prefabs/Vobs/zCVobSoundDaytime",
                PrefabType.VobLadder => "Prefabs/Vobs/oCMobLadder",
                PrefabType.VobLight => "Prefabs/Vobs/zCVobLight",

                PrefabType.VobItem => "Prefabs/Vobs/oCItem",
                PrefabType.VobItemLockPick => "Prefabs/Vobs/oCItem/LockPick",
                
                PrefabType.Player => "Prefabs/Player",
                PrefabType.MainMenu => "Prefabs/Menus/MainMenu",
                PrefabType.XRDeviceSimulator => "Prefabs/VRPlayer/XR Device Simulator",
                PrefabType.StoryIntroduceChapter => "Prefabs/Story/IntroduceChapter",
                _ => throw new Exception($"Enum value {type} not yet defined.")
            };
        }
    }
}
