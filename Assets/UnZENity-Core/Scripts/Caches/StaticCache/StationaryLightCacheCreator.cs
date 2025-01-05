using System.Collections.Generic;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using UnityEngine;
using ZenKit.Vobs;
using Light = ZenKit.Vobs.Light;

namespace GUZ.Core.Caches.StaticCache
{
    public class StationaryLightCacheCreator
    {
        public List<StaticCacheManager.StationaryLightInfo> StationaryLightInfos = new();

        public void CalculateStationaryLights(List<IVirtualObject> vobs)
        {
            foreach (var vob in vobs)
            {
                if (vob.Type != VirtualObjectType.zCVobLight)
                {
                    // FIXME - If we have a fire, we need to load the .zen file and loop through its children! Test if it works below Orry in start area.
                    CalculateStationaryLights(vob.Children);
                    continue;
                }

                // TODO - Check if we need to look for "isStatic == true"?

                var light = (Light)vob;
                var unityPosition = light.Position.ToUnityVector();
                var linearColor = new Color(light.Color.R / 255f, light.Color.G / 255f, light.Color.B / 255f, light.Color.A / 255f).linear;

                StationaryLightInfos.Add(new StaticCacheManager.StationaryLightInfo(unityPosition, light.Range, linearColor));

                CalculateStationaryLights(vob.Children);
            }
        }
    }
}
