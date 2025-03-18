using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Npc;
using GUZ.Core.Vm;
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
#if UNITY_EDITOR
        public IOrderedEnumerable<AnimationTrackInstance> DebugTrackInstances => _trackInstances.OrderBy(i => i.Track.Layer);
        public string[] DebugBoneNames => _boneNames;
#endif

        public Transform RootBone;

        // Caching bone Transforms makes it faster to apply them to animations later.
        private string[] _boneNames;
        private Transform[] _bones;
        private List<AnimationTrackInstance> _trackInstances = new();

        protected override void Awake()
        {
            base.Awake();

            // Cached object which will be used later.
            NpcData.PrefabProps.AnimationSystem = this;
        }

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

        public bool PlayAnimation(string animationName)
        {
            var newTrack = AnimationManager2.GetTrack(animationName, Properties.MdsNameBase, Properties.MdsNameOverlay);

            if (newTrack == null)
            {
                return false;
            }

            // FIXME - Now we need to handle animation flags: M - Move and R - Rotate.
            //         Then S_ROTATEL will work properly and stop once rotated enough.
            Debug.Log("Playing animation: " + animationName + "");

            if (IsAlreadyPlaying(newTrack))
            {
                return true;
            }

            var newTrackInstance = new AnimationTrackInstance(newTrack);
            var newTrackLayer = newTrackInstance.Track.Layer;

            // Handle existing Track Blending based on layer of new Track.
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var trackInstance = _trackInstances[i];
                var trackLayer = trackInstance.Track.Layer;

                if (trackLayer < newTrackLayer)
                {
                    BlendOutTrackBones(trackInstance, newTrackInstance);
                }
                else if (trackLayer == newTrackLayer)
                {
                    BlendOutTrack(trackInstance, newTrackInstance);
                }
                else if (trackLayer > newTrackLayer)
                {

                }
            }

            // If this is the first animation on NPC, we need no BlendIn, simply start all bones at frame 0.
            // TODO - Could be handled differently as a Layer0 animation aka Bone pose of ModelMesh itself.
            if (_trackInstances.IsEmpty())
            {
                newTrackInstance.SetPlayState();
            }

            StopTrackBones(newTrackInstance);
            _trackInstances.Add(newTrackInstance);

            return true;
        }

        private bool IsAlreadyPlaying(AnimationTrack newTrack)
        {
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                if (newTrack.Animation.Name == _trackInstances[i].Track.Animation.Name)
                {
                    return true;
                }
            }

            return false;
        }

        public bool PlayIdleAnimation()
        {
            return PlayAnimation(GetIdleAnimationName());
        }

        public float GetAnimationDuration(string animationName)
        {
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var instance = _trackInstances[i];
                if (instance.Track.Animation.Name.EqualsIgnoreCase(animationName))
                {
                    return instance.Track.Duration;
                }
            }

            return 0f;
        }

        public void StopAnimation(string stoppingAnimationName)
        {
            AnimationTrackInstance instanceToStop = null;

            // Fetch and blend out Animation.
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var instance = _trackInstances[i];
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
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var instance = _trackInstances[i];
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
        private void BlendOutTrackBones(AnimationTrackInstance lowerLayerTrack, AnimationTrackInstance higherLayerTrack)
        {
            lowerLayerTrack.BlendOutBones(higherLayerTrack.Track.BoneNames, higherLayerTrack.Track.Animation.BlendIn);
        }

        /// <summary>
        /// Tracks on the same layer will either need to stop immediately or blend out at the current frame.
        /// </summary>
        private void BlendOutTrack(AnimationTrackInstance oldTrack, AnimationTrackInstance newTrack)
        {
            // From Documentation:
            // E: Diese Flag sorgt dafür, dass die Ani erst gestartet wird, wenn eine zur Zeit aktive Ani im selben Layer ihren letzten Frame
            // erreicht hat und somit beendet wird. Sinnvoll z.B. in folgenden Fall: ani "s_walk", ani "t_walk_2_stand", ani "s_stand", wobei alle Anis als ASC-Anis vorliegen.
            var isStartAtLastFrame = newTrack.Track.Animation.Flags.HasFlag(AnimationFlags.Queue);

            if (isStartAtLastFrame)
            {
                // FIXME - Implement
                Debug.LogError("AnimationFlas.Queue not implemented yet.");
            }
            // else
            // {
                oldTrack.BlendOutTrack(newTrack.Track.Animation.BlendIn);
            // }
        }

        /// <summary>
        /// The current instance starts blending out, which means, that lower layer bones can blend in again.
        /// </summary>
        private void BlendInOtherTrackBones(AnimationTrackInstance instanceBlendingOut)
        {
            foreach (var trackInstance in _trackInstances)
            {
                if (trackInstance.Track.Layer < instanceBlendingOut.Track.Layer)
                {
                    trackInstance.BlendInBones(instanceBlendingOut.Track.BoneNames, instanceBlendingOut.Track.Animation.BlendOut);
                }
            }
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
                switch (instance.Update(Time.deltaTime))
                {
                    case AnimationState.None:
                        break;
                    case AnimationState.BlendIn:
                        break;
                    case AnimationState.Play:
                        break;
                    case AnimationState.BlendOut:
                        BlendInOtherTrackBones(instance);
                        break;
                    case AnimationState.Stop:
                        _trackInstances.Remove(instance);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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

                var boneWeightSum = 0f;
                foreach (var track in _trackInstances)
                {
                    if (track.TryGetBonePose(boneName, out var position, out var rotation, out var weight))
                    {
                        finalPosition += position;
                        finalRotation *= rotation;
                        hasBoneAnimation = true;
                        boneWeightSum += weight;
                    }
                }

                if (!Mathf.Approximately(boneWeightSum, 1f))
                {
                    Debug.Break();
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

        private string GetIdleAnimationName()
        {
            string walkMode;
            switch (Properties.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    walkMode = "WALK";
                    break;
                case VmGothicEnums.WalkMode.Run:
                    walkMode = "RUN";
                    break;
                case VmGothicEnums.WalkMode.Sneak:
                    walkMode = "SNEAK";
                    break;
                case VmGothicEnums.WalkMode.Water:
                    walkMode = "WATER";
                    break;
                case VmGothicEnums.WalkMode.Swim:
                    walkMode = "SWIM";
                    break;
                case VmGothicEnums.WalkMode.Dive:
                    walkMode = "DIVE";
                    break;
                default:
                    Debug.LogWarning($"Animation of type {Properties.WalkMode} not yet implemented.");
                    return "";
            }

            return $"S_{walkMode}";
        }
    }
}
