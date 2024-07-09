using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace GUZ.Core.Manager.Culling
{
    public class NpcMeshCullingManager
    {
        private readonly bool _featureEnableCulling;
        private readonly float _featureCullingDistance;
        private readonly ICoroutineManager _coroutineManager;

        // Stored for resetting after world switch
        private CullingGroup _npcCullingGroup;

        // Stored for later index mapping SphereIndex => GOIndex
        private readonly List<GameObject> _objects = new();


        public NpcMeshCullingManager(GameConfiguration config, ICoroutineManager coroutineManager)
        {
            _featureEnableCulling = config.EnableNPCMeshCulling;
            _featureCullingDistance = config.NPCCullingDistance;
            _coroutineManager = coroutineManager;
        }

        public void Init()
        {
            if (!_featureEnableCulling)
            {
                return;
            }

            GlobalEventDispatcher.GeneralSceneUnloaded.AddListener(PreWorldCreate);
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(PostWorldCreate);

            // Unity demands CullingGroups to be created in Awake() or Start() earliest.
            _npcCullingGroup = new CullingGroup();
        }

        public void PreWorldCreate()
        {
            _npcCullingGroup.Dispose();
            _npcCullingGroup = new CullingGroup();
            _objects.Clear();
        }

        /// <summary>
        /// Fill CullingGroups with GOs based on size (radius) and position
        /// </summary>
        public void PrepareSoundCulling([ItemCanBeNull] List<GameObject> gameObjects)
        {
            if (!_featureEnableCulling)
            {
                return;
            }

            var spheres = new List<BoundingSphere>();

            foreach (var go in gameObjects.Where(i => i != null))
            {
                if (go == null)
                {
                    continue;
                }

                _objects.Add(go);
                var sphere = new BoundingSphere(go.transform.position, go.GetComponent<AudioSource>().maxDistance);
                spheres.Add(sphere);
            }

            // Disable sounds if we're leaving the area and therefore last audible location.
            // Hint: As there are non spatial sounds (always same volume wherever we are),
            // we need to disable the sounds at exactly the spot we are.
            _npcCullingGroup.SetBoundingDistances(new[] { _featureCullingDistance });
            _npcCullingGroup.onStateChanged = NPCVisibilityChanged;
            _npcCullingGroup.SetBoundingSpheres(spheres.ToArray());
        }

        private void NPCVisibilityChanged(CullingGroupEvent evt)
        {
            // Ignore Frustum and Occlusion culling.
            if (evt.previousDistance == evt.currentDistance)
            {
                return;
            }

            var inVisibleRange = evt.previousDistance > evt.currentDistance;

            _objects[evt.index].SetActive(inVisibleRange);
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        public void PostWorldCreate(GameObject playerGo)
        {
            var mainCamera = Camera.main!;
            _npcCullingGroup.targetCamera = mainCamera;
            _npcCullingGroup.SetDistanceReferencePoint(mainCamera.transform);
        }

        public void Destroy()
        {
            if (!_featureEnableCulling)
            {
                return;
            }

            _npcCullingGroup.Dispose();
        }
    }
}
