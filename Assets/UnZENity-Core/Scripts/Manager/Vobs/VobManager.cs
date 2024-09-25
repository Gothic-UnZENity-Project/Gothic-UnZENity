using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Extensions;
using UnityEngine;
using ZenKit.Vobs;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager.Vobs
{
    public class VobManager : AbstractVobManager
    {
        private List<VirtualObjectType> _featureTypesToSpawn;


        public VobManager(GameConfiguration config)
        {
            FeatureEnableSounds = config.EnableGameSounds;
            FeatureEnableMusic = config.EnableGameMusic;
            FeatureShowFreePoints = config.ShowFreePoints;
            FeatureEnableNpcs = config.EnableNpcs;
            FeatureShowParticleEffects = config.EnableParticleEffects;
            FeatureShowDecals = config.EnableDecalVisuals;

            _featureTypesToSpawn = config.SpawnVOBTypes.Value.ToList();

            GlobalEventDispatcher.WorldSceneLoaded.AddListener(PostWorldLoaded);
        }

        private void PostWorldLoaded()
        {
            GameContext.InteractionAdapter.SetTeleportationArea(TeleportParentGo);
        }

        public async Task CreateAsync(LoadingManager loading, List<IVirtualObject> vobs, GameObject rootGo)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            PreCreateVobs(vobs, rootGo);
            await CreateVobs(loading, vobs);
            PostCreateVobs();
            stopwatch.Stop();
            Debug.Log($"Created vobs in {stopwatch.Elapsed.TotalSeconds} s");
        }

        public GameObject GetRootGameObjectOfType(VirtualObjectType type)
        {
            GameObject retVal;

            if (ParentGosTeleport.TryGetValue(type, out retVal))
            {
                return retVal;
            }
            else if (ParentGosNonTeleport.TryGetValue(type, out retVal))
            {
                return ParentGosNonTeleport[type];
            }
            else
            {
                Debug.LogError($"No suitable root GO found for type >{type}<");
                return null;
            }
        }

        protected override void PreCreateVobs(List<IVirtualObject> vobs, GameObject rootGo)
        {
            // The elements are already created by glTF cache.
            TeleportParentGo = rootGo.transform.Find("Teleport").gameObject;
            NonTeleportParentGo = rootGo.transform.Find("NonTeleport").gameObject;

            TotalVObs = GetTotalVobCount(vobs);

            CreatedCount = 0;
            CullingVobObjects.Clear();
        }

        protected override void PostCreateVobs()
        {
            GameGlobals.VobMeshCulling.PrepareVobCulling(CullingVobObjects);

            FireTreeCache.ClearAndReleaseMemory();

            // TODO - warnings about "not implemented" - print them once only.
            foreach (var var in new[]
                     {
                         VirtualObjectType.zCVobScreenFX,
                         VirtualObjectType.zCVobAnimate,
                         VirtualObjectType.zCTriggerWorldStart,
                         VirtualObjectType.zCTriggerList,
                         VirtualObjectType.oCCSTrigger,
                         VirtualObjectType.oCTriggerScript,
                         VirtualObjectType.zCVobLensFlare,
                         VirtualObjectType.zCMoverController,
                         VirtualObjectType.zCPFXController
                     })
            {
                Debug.LogWarning($"{var} not yet implemented.");
            }
        }

        protected override bool SpawnObjectType(VirtualObjectType type)
        {
            return _featureTypesToSpawn.IsEmpty() || _featureTypesToSpawn.Contains(type);
        }
    }
}
