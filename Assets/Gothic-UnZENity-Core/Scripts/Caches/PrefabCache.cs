using System;
using System.Collections.Generic;
using GUZ.Core.Context;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GUZ.Core.Caches
{
    public static class PrefabCache
    {
        private static readonly Dictionary<PrefabType, GameObject> Cache = new();

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

        private static string GetPath(PrefabType type)
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

        /// <summary>
        /// Lookup is done in following places:
        /// 1. CONTEXT_NAME/Prefabs/... - overwrites lookup path below, used for specific prefabs, for current context (HVR, Flat, ...)
        /// 2. Prefabs/... - Located inside core module (GVR), if we don't need special handling.
        /// </summary>
        public static GameObject TryGetObject(PrefabType type)
        {
            var resourcePath = GetPath(type);
            var contextPrefixPath = $"{GUZContext.InteractionAdapter.GetContextName()}/{resourcePath}";

            foreach (var path in new[]{contextPrefixPath, resourcePath})
            {
                var newPrefab = Resources.Load<GameObject>(path);

                if (newPrefab == null)
                    continue;

                Cache[type] = newPrefab;
                return Object.Instantiate(newPrefab);
            }

            throw new ArgumentException($"No suitable prefab found for >{type}<");
        }

        public static void Dispose()
        {
            Cache.Clear();
        }
    }
}
