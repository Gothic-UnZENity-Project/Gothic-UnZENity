using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace GUZ.Core.Manager.Culling
{
    public class VobSoundCullingManager
    {
        // Stored for resetting after world switch
        private CullingGroup _soundCullingGroup;

        // Stored for later index mapping SphereIndex => GOIndex
        private readonly List<GameObject> _objects = new();

        private readonly bool _featureEnable;

        public VobSoundCullingManager(GameConfiguration config)
        {
            _featureEnable = config.EnableSoundCulling;
        }

        public void Init()
        {
            if (!_featureEnable)
            {
                return;
            }

            GlobalEventDispatcher.GeneralSceneUnloaded.AddListener(PreWorldCreate);
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(PostWorldCreate);

            // Unity demands CullingGroups to be created in Awake() or Start() earliest.
            _soundCullingGroup = new CullingGroup();
        }

        public void PreWorldCreate()
        {
            _soundCullingGroup.Dispose();
            _soundCullingGroup = new CullingGroup();
            _objects.Clear();
        }

        private void SoundChanged(CullingGroupEvent evt)
        {
            // Ignore Frustum and Occlusion culling.
            if (evt.previousDistance == evt.currentDistance)
            {
                return;
            }

            var inAudibleRange = evt.previousDistance > evt.currentDistance;

            _objects[evt.index].SetActive(inAudibleRange);
        }


        /// <summary>
        /// Fill CullingGroups with GOs based on size (radius) and position
        /// </summary>
        public void PrepareSoundCulling([ItemCanBeNull] List<GameObject> gameObjects)
        {
            if (!_featureEnable)
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
            _soundCullingGroup.SetBoundingDistances(new[] { 0f });
            _soundCullingGroup.onStateChanged = SoundChanged;
            _soundCullingGroup.SetBoundingSpheres(spheres.ToArray());
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        public void PostWorldCreate(GameObject playerGo)
        {
            var mainCamera = Camera.main!;
            _soundCullingGroup.targetCamera = mainCamera;
            _soundCullingGroup.SetDistanceReferencePoint(mainCamera.transform);
        }

        public void Destroy()
        {
            if (!_featureEnable)
            {
                return;
            }

            _soundCullingGroup.Dispose();
        }
    }
}
