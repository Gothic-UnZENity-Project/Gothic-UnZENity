using System;
using GUZ.Core.Adapters.Properties.Vob;
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Extensions;
using GUZ.Core.Const;
using GUZ.Core.Core.Logging;
using GUZ.Core.Services.Caches;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.Rendering;
using ZenKit.Vobs;
using Logger = GUZ.Core.Core.Logging.Logger;

namespace GUZ.Core.Domain.Meshes.Builder
{
    /// <summary>
    /// Please check description at worldofgothic for more details:
    /// https://www.worldofgothic.de/modifikation/index.php?go=particelfx
    /// </summary>
    public class VobPfxMeshBuilder : AbstractMeshBuilder
    {
        private IVirtualObject _vob;

        public void SetPfxData(IVirtualObject vob)
        {
            _vob = vob;
        }

        public override GameObject Build()
        {
            var pfxGo = ResourceLoader.TryGetPrefabObject(PrefabType.VobPfx);
            pfxGo.name = _vob.Visual!.Name;
            pfxGo.SetParent(ParentGo);

            var pfx = VmCacheService.TryGetPfxData(_vob.Visual!.Name);
            var particleSystem = pfxGo.GetComponent<ParticleSystem>();

            pfxGo.GetComponent<VobPfxProperties>().PfxData = pfx;

            particleSystem.Stop();

            var gravity = pfx.FlyGravityS.Split();
            float gravityX = 1f, gravityY = 1f, gravityZ = 1f;
            if (gravity.Length == 3)
            {
                // Gravity seems too low. Therefore *10k.
                gravityX = float.Parse(gravity[0]) * 10000;
                gravityY = float.Parse(gravity[1]) * 10000;
                gravityZ = float.Parse(gravity[2]) * 10000;
            }

            // Main module
            {
                var mainModule = particleSystem.main;
                // I assume we need to change milliseconds to seconds.
                var minLifeTime = (pfx.LspPartAvg - pfx.LspPartVar) / 1000;
                var maxLifeTime = (pfx.LspPartAvg + pfx.LspPartVar) / 1000;
                mainModule.duration = 1f; // I assume pfx data wants a cycle being 1 second long.
                mainModule.startLifetime = new ParticleSystem.MinMaxCurve(minLifeTime, maxLifeTime);
                mainModule.loop = Convert.ToBoolean(pfx.PpsIsLooping);

                var minSpeed = (pfx.VelAvg - pfx.VelVar) / 1000;
                var maxSpeed = (pfx.VelAvg + pfx.VelVar) / 1000;
                mainModule.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            }

            // Emission module
            {
                var emissionModule = particleSystem.emission;
                emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(pfx.PpsValue);
            }

            // Force over Lifetime module
            {
                var forceModule = particleSystem.forceOverLifetime;
                if (gravity.Length == 3)
                {
                    forceModule.enabled = true;
                    forceModule.x = gravityX;
                    forceModule.y = gravityY;
                    forceModule.z = gravityZ;
                }
            }

            // Color over Lifetime module
            {
                var colorOverTime = particleSystem.colorOverLifetime;
                colorOverTime.enabled = true;
                var gradient = new Gradient();
                var colorStart = pfx.VisTexColorStartS.Split();
                var colorEnd = pfx.VisTexColorEndS.Split();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new(
                            new Color(float.Parse(colorStart[0]) / 255, float.Parse(colorStart[1]) / 255,
                                float.Parse(colorStart[2]) / 255),
                            0f),
                        new(
                            new Color(float.Parse(colorEnd[0]) / 255, float.Parse(colorEnd[1]) / 255,
                                float.Parse(colorEnd[2]) / 255), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new(pfx.VisAlphaStart / 255, 0),
                        new(pfx.VisAlphaEnd / 255, 1)
                    });
                colorOverTime.color = gradient;
            }

            // Size over lifetime module
            {
                var sizeOverTime = particleSystem.sizeOverLifetime;
                sizeOverTime.enabled = true;

                var curve = new AnimationCurve();
                var shapeScaleKeys = pfx.ShpScaleKeysS.Split();
                if (shapeScaleKeys.Length > 1 && !pfx.ShpScaleKeysS.IsEmpty())
                {
                    var curveTime = 0f;

                    foreach (var key in shapeScaleKeys)
                    {
                        curve.AddKey(curveTime, float.Parse(key) / 100 * float.Parse(pfx.ShpDimS));
                        curveTime += 1f / shapeScaleKeys.Length;
                    }

                    sizeOverTime.size = new ParticleSystem.MinMaxCurve(1f, curve);
                }
            }

            // Renderer module
            {
                var rendererModule = pfxGo.GetComponent<ParticleSystemRenderer>();
                // FIXME - Move to a cached constant value
                var standardShader = Constants.ShaderUnlitParticles;
                var material = new Material(standardShader);
                rendererModule.material = material;
                GameGlobals.Textures.SetTexture(pfx.VisNameS, rendererModule.material);
                // renderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest; // First check with no change.

                switch (pfx.VisAlphaFuncS.ToUpper())
                {
                    case "BLEND":
                        rendererModule.material.ToTransparentMode(); // e.g. leaves.pfx.
                        break;
                    case "ADD":
                        rendererModule.material.ToAdditiveMode();
                        break;
                    default:
                        Logger.LogWarning($"Particle AlphaFunc {pfx.VisAlphaFuncS} not yet handled.", LogCat.Mesh);
                        break;
                }

                // makes the material render both faces
                rendererModule.material.SetInt("_Cull", (int)CullMode.Off);

                switch (pfx.VisOrientationS)
                {
                    case "NONE":
                        rendererModule.alignment = ParticleSystemRenderSpace.View;
                        break;
                    case "WORLD":
                        rendererModule.alignment = ParticleSystemRenderSpace.World;
                        break;
                    case "VELO":
                        rendererModule.alignment = ParticleSystemRenderSpace.Velocity;
                        break;
                    default:
                        Logger.LogWarning($"visOrientation {pfx.VisOrientationS} not yet handled.", LogCat.Mesh);
                        break;
                }
            }

            // Shape module
            {
                var shapeModule = particleSystem.shape;
                switch (pfx.ShpTypeS.ToUpper())
                {
                    case "SPHERE":
                        shapeModule.shapeType = ParticleSystemShapeType.Sphere;
                        break;
                    case "CIRCLE":
                        shapeModule.shapeType = ParticleSystemShapeType.Circle;
                        break;
                    case "MESH":
                        shapeModule.shapeType = ParticleSystemShapeType.Mesh;
                        break;
                    default:
                        Logger.LogWarning($"Particle ShapeType {pfx.ShpTypeS} not yet handled.", LogCat.Mesh);
                        break;
                }

                var shapeDimensions = pfx.ShpDimS.Split();
                switch (shapeDimensions.Length)
                {
                    case 1:
                        // cm in m
                        shapeModule.radius = float.Parse(shapeDimensions[0]) / 100;
                        break;
                    default:
                        Logger.LogWarning($"shpDim >{pfx.ShpDimS}< not yet handled", LogCat.Mesh);
                        break;
                }

                shapeModule.rotation = new Vector3(pfx.DirAngleElev, 0, 0);

                var shapeOffsetVec = pfx.ShpOffsetVecS.Split();
                if (float.TryParse(shapeOffsetVec[0], out var x) && float.TryParse(shapeOffsetVec[1], out var y) &&
                    float.TryParse(shapeOffsetVec[2], out var z))
                {
                    shapeModule.position = new Vector3(x / 100, y / 100, z / 100);
                }

                shapeModule.alignToDirection = true;

                shapeModule.radiusThickness = Convert.ToBoolean(pfx.ShpIsVolume) ? 1f : 0f;
            }

            particleSystem.Play();

            return pfxGo;
        }
    }
}
