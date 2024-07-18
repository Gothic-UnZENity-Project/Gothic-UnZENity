using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Caches;
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


        public VobSoundCullingManager(GameConfiguration config)
        {
            // NOP
        }

        public void Init()
        {
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
            // Hint: As there are non-spatial sounds (always same volume wherever we are),
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
            /*
             * We need to check for all Sounds once, if they need to be activated as they're next to player.
             * As CullingGroup only triggers deactivation once player spawns, but no activation.
             */
            var loc = Camera.main!.transform.position;
            foreach (var sound in LookupCache.VobSoundsAndDayTime.Where(i => i != null))
            {
                var soundLoc = sound.transform.position;
                var soundDist = sound.GetComponent<AudioSource>().maxDistance;
                var dist = Vector3.Distance(loc, soundLoc);

                if (dist < soundDist)
                {
                    sound.SetActive(true);
                }
            }

            var mainCamera = Camera.main!;
            _soundCullingGroup.targetCamera = mainCamera;
            _soundCullingGroup.SetDistanceReferencePoint(mainCamera.transform);
        }

        public void Destroy()
        {
            _soundCullingGroup.Dispose();
        }
    }
}
