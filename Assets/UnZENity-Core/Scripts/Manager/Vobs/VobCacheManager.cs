using System;
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
    public class VobCacheManager : AbstractVobManager
    {
        public static readonly List<VirtualObjectType> VobTypesToCache = new()
        {
            VirtualObjectType.oCMobBed,
            VirtualObjectType.oCMobContainer,
            VirtualObjectType.oCMobDoor,
            VirtualObjectType.oCMobFire,
            VirtualObjectType.oCMobInter,
            VirtualObjectType.zCVob,
            VirtualObjectType.oCMobLadder,
            VirtualObjectType.oCMobSwitch,
            VirtualObjectType.oCMobWheel,
            VirtualObjectType.zCVobAnimate,
            VirtualObjectType.zCVobLight, // Important at caching time as it will add StationaryLight components to calculate world mesh chunks later.
            VirtualObjectType.zCVobStair,
            VirtualObjectType.oCMOB
        };

        protected override bool SpawnObjectType(VirtualObjectType type)
        {
            return VobTypesToCache.Contains(type);
        }

        public async Task CreateForCache(List<IVirtualObject> rootVobs, LoadingManager loading, GameObject rootGo)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            PreCreateVobs(rootVobs, rootGo);
            await CreateVobs(loading, rootVobs);
            stopwatch.Stop();

            Debug.Log($"Created VOBs for caching in {stopwatch.Elapsed.TotalSeconds} s");
        }

        private void PreCreateVobs(List<IVirtualObject> rootVobs, GameObject rootGo)
        {
            TotalVObs = GetTotalVobCount(rootVobs);
            CreatedCount = 0;

            TeleportParentGo = new GameObject("Teleport");
            NonTeleportParentGo = new GameObject("NonTeleport");
            TeleportParentGo.SetParent(rootGo);
            NonTeleportParentGo.SetParent(rootGo);

            ParentGosTeleport = new Dictionary<VirtualObjectType, GameObject>();
            ParentGosNonTeleport = new Dictionary<VirtualObjectType, GameObject>();

            CreateParentVobObjectTeleport();
            CreateParentVobObjectNonTeleport();
        }

        private void PostCreateVobs()
        {
            // Nothing to do for now.
        }

        /// <summary>
        /// During caching, we only want to cache standard Gothic data. It means we will never ever load additional prefab data into the VOBs.
        ///
        /// Justification:
        /// * The caching mechanism should be stable after created once. If we alter a prefab, we always need to tell our players: Recreate cache.
        /// * We don't know if there will be any side effects if we cache additional GOs and Components.
        /// </summary>
        protected override GameObject GetPrefab(IVirtualObject vob)
        {
            return new GameObject();
        }

        protected override void AddToMobInteractableList(IVirtualObject vob, GameObject go)
        {
            // Nothing to do at caching stage
        }

        private void CreateParentVobObjectTeleport()
        {
            var allTypes = (VirtualObjectType[])Enum.GetValues(typeof(VirtualObjectType));
            foreach (var type in allTypes.Except(NonTeleportTypes))
            {
                var newGo = new GameObject(type.ToString());
                newGo.SetParent(TeleportParentGo);

                ParentGosTeleport.Add(type, newGo);
            }
        }

        /// <summary>
        /// As PxVobType.PxVob_oCItem get Grabbable Component, they already own a Collider
        /// AND we don't want to teleport on top of them. We therefore exclude them from being added to Teleporter.
        /// </summary>
        private void CreateParentVobObjectNonTeleport()
        {
            var allTypes = (VirtualObjectType[])Enum.GetValues(typeof(VirtualObjectType));
            foreach (var type in allTypes.Intersect(NonTeleportTypes))
            {
                var newGo = new GameObject(type.ToString());
                newGo.SetParent(NonTeleportParentGo);

                ParentGosNonTeleport.Add(type, newGo);
            }
        }
    }
}
