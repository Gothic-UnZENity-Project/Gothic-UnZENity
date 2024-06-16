using System;

namespace GUZ.Core
{
	public enum PrefabType
	{
		Npc,
		WayPoint,
		Vob,
		VobAnimate,
		VobItem,
		VobContainer,
		VobDoor,
		VobInteractable,
		VobMovable,
		VobSpot,
		VobPfx,
		VobMusic,
		VobSound,
		VobSoundDaytime,
		VobLadder,
		XRDeviceSimulator
	}

	public static class PrefabTypeExtension
	{
		public static string Path(this PrefabType type)
		{
			return type switch
			{
				PrefabType.Npc => "Prefabs/Npc",
				PrefabType.WayPoint => "Prefabs/WayPoint",
				PrefabType.Vob => "Prefabs/Vobs/Vob",
				PrefabType.VobAnimate => "Prefabs/Vobs/zCVobAnimate",
				PrefabType.VobItem => "Prefabs/Vobs/oCItem",
				PrefabType.VobContainer => "Prefabs/Vobs/oCMobContainer",
				PrefabType.VobDoor => "Prefabs/Vobs/oCMobDoor",
				PrefabType.VobInteractable => "Prefabs/Vobs/oCMobInter",
				PrefabType.VobMovable => "Prefabs/Vobs/oCMobMovable",
				PrefabType.VobSpot => "Prefabs/Vobs/zCVobSpot",
				PrefabType.VobPfx => "Prefabs/Vobs/vobPfx",
				PrefabType.VobMusic => "Prefabs/Vobs/oCZoneMusic",
				PrefabType.VobSound => "Prefabs/Vobs/zCVobSound",
				PrefabType.VobSoundDaytime => "Prefabs/Vobs/zCVobSoundDaytime",
				PrefabType.VobLadder => "Prefabs/Vobs/oCMobLadder",
				PrefabType.XRDeviceSimulator => "Prefabs/XR Device Simulator",
				_ => throw new Exception($"Enum value {type} not yet defined.")
			};
		}
	}
}
