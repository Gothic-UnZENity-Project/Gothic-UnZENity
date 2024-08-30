using System;

namespace GUZ.Core
{
    public enum PrefabType
    {
        WayPoint,
        Vob,
        Npc,
        VobAnimate,
        VobItem,
        VobContainer,
        VobDoor,
        VobFire,
        VobBed,
        VobWheel,
        VobSwitch,
        VobInteractable,
        VobMovable,
        VobSpot,
        VobPfx,
        VobMusic,
        VobSound,
        VobSoundDaytime,
        VobLadder,

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
                PrefabType.WayPoint => "Prefabs/WayPoint",
                PrefabType.Vob => "Prefabs/Vobs/Vob",
                PrefabType.Npc => "Prefabs/Vobs/oCNpc",
                PrefabType.VobAnimate => "Prefabs/Vobs/zCVobAnimate",
                PrefabType.VobItem => "Prefabs/Vobs/oCItem",
                PrefabType.VobContainer => "Prefabs/Vobs/oCMobContainer",
                PrefabType.VobDoor => "Prefabs/Vobs/oCMobDoor",
                PrefabType.VobFire => "Prefabs/Vobs/oCMobFire",
                PrefabType.VobBed => "Prefabs/Vobs/oCMobBed",
                PrefabType.VobWheel => "Prefabs/Vobs/oCMobWheel",
                PrefabType.VobSwitch => "Prefabs/Vobs/oCMobSwitch",
                PrefabType.VobInteractable => "Prefabs/Vobs/oCMobInter",
                PrefabType.VobMovable => "Prefabs/Vobs/oCMobMovable",
                PrefabType.VobSpot => "Prefabs/Vobs/zCVobSpot",
                PrefabType.VobPfx => "Prefabs/Vobs/vobPfx",
                PrefabType.VobMusic => "Prefabs/Vobs/oCZoneMusic",
                PrefabType.VobSound => "Prefabs/Vobs/zCVobSound",
                PrefabType.VobSoundDaytime => "Prefabs/Vobs/zCVobSoundDaytime",
                PrefabType.VobLadder => "Prefabs/Vobs/oCMobLadder",

                PrefabType.Player => "Prefabs/Player",
                PrefabType.MainMenu => "Prefabs/Menus/MainMenu",
                PrefabType.XRDeviceSimulator => "Prefabs/VRPlayer/XR Device Simulator",
                PrefabType.StoryIntroduceChapter => "Prefabs/Story/IntroduceChapter",
                _ => throw new Exception($"Enum value {type} not yet defined.")
            };
        }
    }
}
