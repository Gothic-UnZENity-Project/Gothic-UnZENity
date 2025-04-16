using System.Collections.Generic;
using System.Threading.Tasks;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using MyBox;
using UnityEngine;
using ZenKit;
using ZenKit.Vobs;
using Light = ZenKit.Vobs.Light;

namespace GUZ.Core.Caches.StaticCache
{
    public class StationaryLightCacheCreator
    {
        public List<StaticCacheManager.StationaryLightInfo> StationaryLightInfos = new();

        // Used for world chunk creation.
        public List<Bounds> StationaryLightBounds = new();

        private bool _debugSpeedUpLoading;


        public StationaryLightCacheCreator()
        {
            _debugSpeedUpLoading = GameGlobals.Config.Dev.SpeedUpLoading;
        }

        public async Task CalculateStationaryLights(List<IVirtualObject> vobs, Vector3 parentWorldPosition = default)
        {
            foreach (var vob in vobs)
            {
                if (!_debugSpeedUpLoading)
                {
                    await FrameSkipper.TrySkipToNextFrame();
                }

                var vobWorldPosition = CalculateWorldPosition(parentWorldPosition, vob.Position.ToUnityVector());

                if (vob.Type == VirtualObjectType.zCVobLight)
                {
                    // TODO - Check if we need to look for "isStatic == true"?

                    var light = (Light)vob;

                    // For chunking and shader usage, we need to use the non-static lights (e.g. from a fire) only.
                    // StaticLights will be handled later.
                    if (light.LightStatic)
                    {
                        continue;
                    }

                    var linearColor = new Color(light.Color.R / 255f, light.Color.G / 255f, light.Color.B / 255f, light.Color.A / 255f).linear;
                    // Range/100 --> m (ZenKit) in centimeter (Unity)
                    StationaryLightInfos.Add(new StaticCacheManager.StationaryLightInfo(vobWorldPosition, light.Range / 100, linearColor));

                    // Calculation: Vector3.One (vectorify) * Range / 100 (centimeter to meter in Unity) * 2 (from range (half-width) to width)
                    var boundsSize = Vector3.one * light.Range / 100 * 2;
                    StationaryLightBounds.Add(new Bounds(vobWorldPosition, boundsSize));
                }
                else if (vob.Type == VirtualObjectType.oCMobFire)
                {
                    var fire = (Fire)vob;

                    if (fire.VobTree.IsNullOrEmpty() || (GameContext.GameVersionAdapter.Version == GameVersion.Gothic2 && fire.VobTree == "FIRETREE_LARGE.ZEN"))
                    {
                        continue;
                    }

                    var fireWorldVobs = ResourceLoader.TryGetWorld(fire.VobTree, GameContext.GameVersionAdapter.Version, true)!.RootObjects;

                    // As we loaded the child-VOBs for fire*.zen at this time, we iterate now.
                    await CalculateStationaryLights(fireWorldVobs, vobWorldPosition);
                }

                // Recursion
                await CalculateStationaryLights(vob.Children, vobWorldPosition);
            }
        }

        /// <summary>
        /// Elements like Fire have children which position is relative to parent.
        /// But at least Fire are .zen files where the children have zeroed world positions which we need to handle properly.
        ///
        /// TODO - Rotation isn't handled for positioning right now. I'm not sure if we need it. If so, please add.
        /// </summary>
        private Vector3 CalculateWorldPosition(Vector3 parentPosition, Vector3 localPosition)
        {
            return parentPosition + localPosition;
        }
    }
}
