using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Npc;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Animations
{
    /// <summary>
    /// NPC component to handle animations. The Blending is using the official Gothic animation information:
    /// https://www.worldofgothic.de/modifikation/index.php?go=animationen
    /// </summary>
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

            BlendOutOtherTrackBones(trackInstance);
            BlendOutOtherTracks(trackInstance);

            StopTrackBones(trackInstance);
            _trackInstances.Add(trackInstance);
        }

        public void StopAnimation(string stoppingAnimationName)
        {
            AnimationTrackInstance instanceToStop = null;

            // Fetch and blend out Animation.
            foreach (var instance in _trackInstances)
            {
                // If animation is found, then mark it as "BlendOut"
                if (instance.Track.Animation.Name.EqualsIgnoreCase(stoppingAnimationName))
                {
                    instanceToStop = instance;
                    instance.BlendOutTrack(instance.Track.Animation.BlendOut);
                    // Do not break. We could potentially need to stop multiple instances of the same animation.
                }
            }

            if (instanceToStop == null)
            {
                return;
            }

            // Ramp up bones on animation with lower level as the higher level bones will blend out.
            foreach (var instance in _trackInstances)
            {
                if (instance.Track.Animation.Name.EqualsIgnoreCase(stoppingAnimationName))
                {
                    continue;
                }
                if (instance.Track.Layer < instanceToStop.Track.Layer)
                {
                    instance.BlendInBones(instanceToStop.Track.BoneNames, instanceToStop.Track.Animation.BlendOut);
                }
            }
        }

        /// <summary>
        /// Higher level Animations might have only a few bones which might be handled by a lower layer animation.
        /// We therefore need to blend out the other animation(s) Bones, not the whole animation.
        /// </summary>
        private void BlendOutOtherTrackBones(AnimationTrackInstance newInstance)
        {
            foreach (var trackInstance in _trackInstances)
            {
                if (trackInstance.Track.Layer < newInstance.Track.Layer)
                {
                    trackInstance.BlendOutBones(newInstance.Track.BoneNames, newInstance.Track.Animation.BlendIn);
                }
            }
        }

        /// <summary>
        /// Tracks on the same layer will either need to stop immediately or blend out at the current frame.
        /// </summary>
        private void BlendOutOtherTracks(AnimationTrackInstance newInstance)
        {
            // From Documentation:
            // E: Diese Flag sorgt dafür, dass die Ani erst gestartet wird, wenn eine zur Zeit aktive Ani im selben Layer ihren letzten Frame
            // erreicht hat und somit beendet wird. Sinnvoll z.B. in folgenden Fall: ani "s_walk", ani "t_walk_2_stand", ani "s_stand", wobei alle Anis als ASC-Anis vorliegen.
            var isStartAtLastFrame = newInstance.Track.Animation.Flags.HasFlag(AnimationFlags.Queue);

            if (isStartAtLastFrame)
            {
                // FIXME - Implement
                Debug.LogError("AnimationFlas.Queue not implemented yet.");
            }
            // else
            // {
                foreach (var instance in _trackInstances)
                {
                    if (instance.Track.Layer != newInstance.Track.Layer)
                    {
                        continue;
                    }

                    instance.BlendOutTrack(newInstance.Track.Animation.BlendIn);
                }
            // }
        }

        /// <summary>
        /// If we start a new instance, we need to apply, which bones should not be started, as e.g. T_DIALOGGESTURE_ from
        /// a higher level forced the animation to stop bones.
        /// </summary>
        private void StopTrackBones(AnimationTrackInstance newInstance)
        {
            foreach (var instance in _trackInstances)
            {
                if (newInstance.Track.Layer >= instance.Track.Layer)
                {
                    continue;
                }

                newInstance.BlendOutBones(instance.Track.BoneNames, 0f);
            }
        }


        private void Update()
        {
            if (_trackInstances.Count == 0)
            {
                return;
            }

            // Update all tracks
            foreach (var instance in _trackInstances.ToArray())
            {
                if (instance.State == AnimationState.Stop)
                {
                    _trackInstances.Remove(instance);
                    continue;
                }
                instance.Update(Time.deltaTime);
            }

            // Apply final pose
            ApplyFinalPose();
        }

        private void ApplyFinalPose()
        {
            // Accumulate poses from all tracks
            for (var boneIndex = 0; boneIndex < _boneNames.Length; boneIndex++)
            {
                var boneName = _boneNames[boneIndex];
                var bone = _bones[boneIndex];

                var finalPosition = Vector3.zero;
                var finalRotation = Quaternion.identity;
                var hasBoneAnimation = false;

                foreach (var track in _trackInstances)
                {
                    if (track.TryGetBonePose(boneName, out var position, out var rotation))
                    {
                        finalPosition += position;
                        finalRotation *= rotation;
                        hasBoneAnimation = true;
                    }
                }

                // We apply position change only! if we have some update.
                // Otherwise, e.g. T_DIALOGGESTURE_ will pos+rot the lower body into 0,0,0 (aka stomach).
                if (hasBoneAnimation)
                {
                    bone.localPosition = finalPosition;
                    bone.localRotation = finalRotation;
                }
            }
        }
    }
}
