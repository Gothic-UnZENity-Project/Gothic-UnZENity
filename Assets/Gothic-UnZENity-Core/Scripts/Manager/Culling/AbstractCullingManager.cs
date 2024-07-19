using System.Collections.Generic;
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


        public abstract void AddCullingEntry(GameObject go);
        protected abstract void VisibilityChanged(CullingGroupEvent evt);


        public virtual void Init()
        {
            GlobalEventDispatcher.GeneralSceneUnloaded.AddListener(PreWorldCreate);
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(PostWorldCreate);

            // Unity demands CullingGroups to be created in Awake() or Start() earliest.
            CullingGroup = new CullingGroup();
        }

        protected virtual void PreWorldCreate()
        {
            CullingGroup.Dispose();
            CullingGroup = new CullingGroup();
            Objects.Clear();
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        protected virtual void PostWorldCreate(GameObject playerGo)
        {
            // Set main camera as reference point
            var mainCamera = Camera.main!;
            CullingGroup.targetCamera = mainCamera;
            CullingGroup.SetDistanceReferencePoint(mainCamera.transform);
        }

        public virtual void Destroy()
        {
            CullingGroup.Dispose();
        }
    }
}
