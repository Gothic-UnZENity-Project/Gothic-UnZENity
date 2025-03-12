using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Npc;
using UnityEngine;

namespace GUZ.Core.Animations
{
    public class AnimationSystem : BasePlayerBehaviour
    {
        public Transform RootBone;

        // Caching bone Transforms makes it faster to apply them to animations later.
        private string[] _boneNames;
        private Transform[] _bones;
        private List<AnimationTrackInstance> _trackInstances = new();

        private void Start()
        {
            Dictionary<string, Transform> bones = new();
            CollectBones(RootBone, bones);

            _boneNames = bones.Keys.ToArray();
            _bones = bones.Values.ToArray();
        }

        private void CollectBones(Transform bone, Dictionary<string, Transform> bones)
        {
            if (bone.name.StartsWith("BIP01"))
            {
                bones.Add(bone.name, bone);
            }

            foreach (Transform child in bone)
            {
                CollectBones(child, bones);
            }
        }

        public void PlayAnimation(string animationName)
        {
            var track = AnimationManager2.GetTrack(animationName, Properties.MdsNameBase, Properties.MdsNameOverlay);
            var trackInstance = new AnimationTrackInstance(track);
            _trackInstances.Add(trackInstance);
        }

        private void Update()
        {
            // Update all tracks
            foreach (var track in _trackInstances)
            {
                track.Update(Time.deltaTime);
            }

            // Apply final pose
            ApplyFinalPose();
        }

        private void ApplyFinalPose()
        {
            Dictionary<string, (Vector3 position, Quaternion rotation)> finalPose = new();

            // Accumulate poses from all tracks
            for (var boneIndex = 0; boneIndex < _boneNames.Length; boneIndex++)
            {
                var boneName = _boneNames[boneIndex];
                var bone = _bones[boneIndex];

                var finalPosition = Vector3.zero;
                var finalRotation = Quaternion.identity;

                foreach (var track in _trackInstances)
                {
                    track.GetBonePose(boneName, out var position, out var rotation);
                    finalPosition += position;
                    finalRotation *= rotation;
                }

                bone.localPosition = finalPosition;
                bone.localRotation = finalRotation;
            }
        }
    }
}
