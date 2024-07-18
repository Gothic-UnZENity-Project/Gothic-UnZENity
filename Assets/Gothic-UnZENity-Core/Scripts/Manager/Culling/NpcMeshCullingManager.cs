using System.Collections.Generic;
using GUZ.Core.Extensions;
using GUZ.Core.Npc;
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

        // Temporary spheres during execution of Wld_InsertNpc() calls.
        private List<BoundingSphere> _tempSpheres = new();

        // Stored for later index mapping SphereIndex => GOIndex
        private readonly List<GameObject> _objects = new();


        public NpcMeshCullingManager(GameConfiguration config, ICoroutineManager coroutineManager)
        {
            _featureEnableCulling = config.EnableNpcMeshCulling;
            _featureCullingDistance = config.NpcCullingDistance;
            _coroutineManager = coroutineManager;
        }

        public void Init()
        {
            GlobalEventDispatcher.GeneralSceneUnloaded.AddListener(PreWorldCreate);
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(PostWorldCreate);

            // Unity demands CullingGroups to be created in Awake() or Start() earliest.
            _npcCullingGroup = new CullingGroup();
        }

        private void PreWorldCreate()
        {
            _npcCullingGroup.Dispose();
            _npcCullingGroup = new CullingGroup();
            _objects.Clear();
        }

        public void AddCullingEntry(GameObject go)
        {
            _objects.Add(go);
            var sphere = new BoundingSphere(go.transform.position, 1f);
            _tempSpheres.Add(sphere);
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        private void PostWorldCreate(GameObject playerGo)
        {
            if (_featureEnableCulling)
            {
                // Set main camera as reference point
                var mainCamera = Camera.main!;
                _npcCullingGroup.targetCamera = mainCamera;
                _npcCullingGroup.SetDistanceReferencePoint(mainCamera.transform);

                // Fill culling information into spawned GOs
                _npcCullingGroup.SetBoundingDistances(new[] { _featureCullingDistance });
                _npcCullingGroup.SetBoundingSpheres(_tempSpheres.ToArray());
                _npcCullingGroup.onStateChanged = NpcVisibilityChanged;
            }
            // If we disabled NPC culling, then we need to render them all now!
            else
            {
                _objects.ForEach(obj => obj.SetActive(true));
            }

            // Cleanup
            _tempSpheres.ClearAndReleaseMemory();
        }

        private void NpcVisibilityChanged(CullingGroupEvent evt)
        {
            // Ignore Frustum and Occlusion culling.
            if (evt.previousDistance == evt.currentDistance)
            {
                return;
            }

            var inVisibleRange = evt.previousDistance > evt.currentDistance;

            Debug.Log($"Culling: {inVisibleRange} - {_objects[evt.index].name}", _objects[evt.index]);

            _objects[evt.index].SetActive(inVisibleRange);

            if (inVisibleRange)
            {
                _objects[evt.index].GetComponent<AiHandler>().ReEnableNpc();
            }
        }

        public void Destroy()
        {
            _npcCullingGroup.Dispose();
        }
    }
}
