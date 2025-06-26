using System.Collections.Generic;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Manager.Culling
{
    public abstract class AbstractCullingManager
    {
        // Stored for resetting after world switch
        protected CullingGroup CullingGroup;

        // Stored for later index mapping SphereIndex => GOIndex
        protected readonly List<GameObject> Objects = new();

        // Temporary spheres during async world loading calls.
        protected List<BoundingSphere> TempSpheres = new();

        protected bool IsFinalized;

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
            TempSpheres.ClearAndReleaseMemory();
            CullingGroup.Dispose();
            CullingGroup = new CullingGroup();

            IsFinalized = false;
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        protected virtual void PostWorldCreate()
        {
            // Set main camera as reference point
            var mainCamera = Camera.main!;
            CullingGroup.targetCamera = mainCamera;
            CullingGroup.SetDistanceReferencePoint(mainCamera.transform);

            IsFinalized = true;
        }

        public virtual void Destroy()
        {
            CullingGroup.Dispose();
        }
    }
}
