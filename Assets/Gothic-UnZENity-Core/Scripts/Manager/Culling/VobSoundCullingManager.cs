using System.Collections.Generic;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Manager.Culling
{
    public class VobSoundCullingManager
    {
        // Stored for resetting after world switch
        private CullingGroup _soundCullingGroup;

        // Stored for later index mapping SphereIndex => GOIndex
        private readonly List<GameObject> _objects = new();

        // Temporary spheres during execution of Wld_InsertNpc() calls.
        private List<BoundingSphere> _tempSpheres = new();

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
            // A higher distance level means "inaudible" as we only leverage: 0 -> in-range; 1 -> out-of-range.
            var inAudibleRange = evt.currentDistance == 0;

            _objects[evt.index].SetActive(inAudibleRange);
        }


        public void AddCullingEntry(GameObject go)
        {
            _objects.Add(go);
            var sphere = new BoundingSphere(go.transform.position, go.GetComponent<AudioSource>().maxDistance);
            _tempSpheres.Add(sphere);
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        public void PostWorldCreate(GameObject playerGo)
        {
            // Set main camera as reference point
            var mainCamera = Camera.main!;
            _soundCullingGroup.targetCamera = mainCamera;
            _soundCullingGroup.SetDistanceReferencePoint(mainCamera.transform);

            // Disable sounds if we're leaving the area and therefore last audible location.
            // Hint: As there are non-spatial sounds (always same volume wherever we are),
            // we need to disable the sounds at exactly the spot we are.
            _soundCullingGroup.SetBoundingDistances(new[] { 0f });
            _soundCullingGroup.SetBoundingSpheres(_tempSpheres.ToArray());
            _soundCullingGroup.onStateChanged = SoundChanged;

            // Cleanup
            _tempSpheres.ClearAndReleaseMemory();
        }

        public void Destroy()
        {
            _soundCullingGroup.Dispose();
        }
    }
}
