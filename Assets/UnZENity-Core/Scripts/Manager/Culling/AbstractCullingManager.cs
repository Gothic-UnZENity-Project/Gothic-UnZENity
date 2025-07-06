using System.Collections.Generic;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Manager.Culling
{
    public abstract class AbstractCullingManager
    {

        protected enum State
        {
            None,
            Loading,
            WorldLoaded
        }

        protected State CurrentState;
        
        // Stored for resetting after world switch
        protected CullingGroup CullingGroup;

        // Stored for later index mapping SphereIndex => GOIndex
        protected readonly List<GameObject> Objects = new();

        // Temporary spheres during async world loading calls.
        protected List<BoundingSphere> Spheres = new();

        protected abstract void VisibilityChanged(CullingGroupEvent evt);


        public virtual void Init()
        {
            GlobalEventDispatcher.LoadGameStart.AddListener(PreWorldCreate);
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(PostWorldCreate);

            // Unity demands CullingGroups to be created in Awake() or Start() earliest.
            CullingGroup = new CullingGroup();
        }

        protected virtual void PreWorldCreate()
        {
            Objects.ClearAndReleaseMemory();
            Spheres.ClearAndReleaseMemory();
            CullingGroup.Dispose();
            CullingGroup = new CullingGroup();

            CurrentState = State.Loading;
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        protected virtual void PostWorldCreate()
        {
            // Set main camera as reference point
            var mainCamera = Camera.main!;
            CullingGroup.targetCamera = mainCamera; // Needed for FrustumCulling and OcclusionCulling to work.
            CullingGroup.SetDistanceReferencePoint(mainCamera.transform); // Needed for BoundingDistances to work.

            CurrentState = State.WorldLoaded;
        }

        public virtual void Destroy()
        {
            CullingGroup.Dispose();
        }
    }
}
