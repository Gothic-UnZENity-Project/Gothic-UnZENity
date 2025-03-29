using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MyBox;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Animations
{
    /// <summary>
    /// Currently playing instance of a Track on a NPC.
    /// </summary>
    public class AnimationTrackInstance
    {
        public int CreationTime;

        public AnimationTrack Track;

        // Value for this specific point in time
        public float CurrentTime;
        public int CurrentKeyFrameIndex;
        public float NextKeyframeTime;
        public AnimationState State;
        public AnimationState[] BoneStates;
        public float[] BoneBlendWeights;
        public float[] BoneBlendTimes;
        public int BoneAmountStatePlay;
        public int BoneAmountStateStop;

        // FIXME - Wrong - according to Docs, once the last frame is reached, the animation blends out at this pos+rot.
        // FIXME - Docs: Anzumerken ist hierbei, dass das Herunterregeln des Einflusses erst beginnt, sobald der letzte Frame der Ani abgespielt worden ist
        public bool IsLooping;

        private bool _didFrameChangeThisUpdate;

        private int _lastExecutedAnimationEvent;
        private int _lastExecutedPfxEvent;
        private int _lastExecutedSfxEvent;
        private int _lastExecutedMorphEvent;

        private int _animationEventsToExecuteThisUpdate;
        private int _pfxEventsToExecuteThisUpdate;
        private int _sfxEventsToExecuteThisUpdate;
        private int _morphEventsToExecuteThisUpdate;


        public AnimationTrackInstance(AnimationTrack track)
        {
            CreationTime = Time.frameCount;
            Track = track;
            State = AnimationState.BlendIn;
            CurrentTime = 0f;
            CurrentKeyFrameIndex = 0;
            NextKeyframeTime = track.FrameTime;

            IsLooping = track.Animation.Name == track.Animation.Next;

            BoneStates = new AnimationState[Track.BoneCount];
            BoneBlendWeights = new float[Track.BoneCount];
            BoneBlendTimes = new float[Track.BoneCount];

            AnimationState initialBoneState;
            float initialBoneWeight;
            // If w have no BlendIn time, we need to set our animation to play fully right from the start.
            if (Track.Animation.BlendIn == 0)
            {
                initialBoneState = AnimationState.Play;
                initialBoneWeight = 1f;
                BoneAmountStatePlay = Track.BoneCount;
                State = AnimationState.Play;
            }
            else
            {
                initialBoneState = AnimationState.BlendIn;
                initialBoneWeight = 0f;
            }

            for (var i = 0; i < Track.BoneCount; i++)
            {
                BoneStates[i] = initialBoneState;
                BoneBlendWeights[i] = initialBoneWeight;
                BoneBlendTimes[i] = Track.Animation.BlendIn;
            }

            BoneAmountStateStop = 0;

            _lastExecutedAnimationEvent = -1;
            _lastExecutedPfxEvent = -1;
            _lastExecutedSfxEvent = -1;
            _lastExecutedMorphEvent = -1;
        }

        /// <summary>
        /// Update animation information at each frame.
        /// Return status change of whole animation if happening now. (e.g. BlendOut).
        /// </summary>
        public AnimationState Update(float deltaTime)
        {
            CurrentTime += deltaTime;

            if (!IsLooping && State == AnimationState.Play && CurrentTime >= Track.Duration)
            {
                BlendOutTrack(Track.Animation.BlendOut);
                return AnimationState.BlendOut;
            }

            UpdateTrackFrame();
            UpdateEvents();
            UpdateBoneWeights(deltaTime);
            return UpdateState();
        }

        private void UpdateTrackFrame()
        {
            _didFrameChangeThisUpdate = false;

            // If the whole track blends ut, we do not proceed further. (Either we're at the last frame already, or another track stopped us).
            if (State == AnimationState.BlendOut)
            {
                return;
            }

            if (CurrentTime >= Track.Duration)
            {
                _didFrameChangeThisUpdate = true;
                // Restart from the beginning
                _lastExecutedAnimationEvent = -1;
                _lastExecutedPfxEvent = -1;
                _lastExecutedSfxEvent = -1;
                _lastExecutedMorphEvent = -1;

                CurrentTime %= Track.Duration;
                CurrentKeyFrameIndex = 0;
                NextKeyframeTime = CurrentKeyFrameIndex * Track.FrameTime;
            }

            if (CurrentTime >= NextKeyframeTime)
            {
                _didFrameChangeThisUpdate = true;

                CurrentKeyFrameIndex = (int)(CurrentTime / Track.FrameTime); // Round down
                if (Track.KeyFrames.Length > CurrentKeyFrameIndex + 1)
                {
                    NextKeyframeTime = (CurrentKeyFrameIndex + 1) * Track.FrameTime;
                }
                else
                {
                    NextKeyframeTime = float.MaxValue;
                }
            }
        }

        /// <summary>
        /// Whenever an animation frame changed, we need to check, if an animation is now in time range and add it to the "Execute" list.
        /// </summary>
        private void UpdateEvents()
        {
            // If we had some executions last frame, we update the last executed event now.
            _lastExecutedAnimationEvent += _animationEventsToExecuteThisUpdate;
            _lastExecutedPfxEvent += _pfxEventsToExecuteThisUpdate;
            _lastExecutedSfxEvent += _sfxEventsToExecuteThisUpdate;
            _lastExecutedMorphEvent += _morphEventsToExecuteThisUpdate;
            _animationEventsToExecuteThisUpdate = 0;
            _pfxEventsToExecuteThisUpdate = 0;
            _sfxEventsToExecuteThisUpdate = 0;
            _morphEventsToExecuteThisUpdate = 0;

            // We need to check for new events, if we moved to another frame only.
            if (!_didFrameChangeThisUpdate)
            {
                return;
            }

            // AnimationEvents
            for (var i = _lastExecutedAnimationEvent + 1; i < Track.Animation.EventTagCount; i++)
            {
                var animationEvent = Track.Animation.EventTags[i];

                // Event values are handled based on frame normalization (e.g. animation frames are 39...49 --> 10 frames)
                //   an event at 49 is then at frameIndex=9
                // event Frame=0 has special handling: use as frame 0 without additional normalization
                var animationEventFrame = animationEvent.Frame == 0 ? 0 : ClampFrame(animationEvent.Frame);

                if (animationEventFrame <= CurrentKeyFrameIndex)
                {
                    Debug.Log($"{_animationEventsToExecuteThisUpdate} - Executing event: at frame {CurrentKeyFrameIndex}");
                    _animationEventsToExecuteThisUpdate++;
                }
                // We passed the events which need to be played this frame.
                else
                {
                    break;
                }
            }

            // PFX
            for (var i = _lastExecutedPfxEvent + 1; i < Track.Animation.ParticleEffectCount; i++)
            {
                var pfxEvent = Track.Animation.ParticleEffects[i];
                var pfxEventFrame = ClampFrame(pfxEvent.Frame);

                if (pfxEventFrame <= CurrentKeyFrameIndex)
                {
                    Debug.Log($"{_pfxEventsToExecuteThisUpdate} - Executing event: at frame {CurrentKeyFrameIndex}");
                    _pfxEventsToExecuteThisUpdate++;
                }
                // We passed the events which need to be played this frame.
                else
                {
                    break;
                }
            }

            // SFX
            for (var i = _lastExecutedSfxEvent + 1; i < Track.Animation.SoundEffectCount; i++)
            {
                var sfxEvent = Track.Animation.SoundEffects[i];
                var sfxEventFrame = ClampFrame(sfxEvent.Frame);

                if (sfxEventFrame <= CurrentKeyFrameIndex)
                {
                    Debug.Log($"{_sfxEventsToExecuteThisUpdate} - Executing event: at frame {CurrentKeyFrameIndex}");
                    _sfxEventsToExecuteThisUpdate++;
                }
                else
                {
                    break;
                }
            }

            // MorphEvents
            for (var i = _lastExecutedMorphEvent + 1; i < Track.Animation.MorphAnimationCount; i++)
            {
                var morphEvent = Track.Animation.MorphAnimations[i];
                var morphEventFrame = ClampFrame(morphEvent.Frame);

                if (morphEventFrame <= CurrentKeyFrameIndex)
                {
                    Debug.Log($"{_morphEventsToExecuteThisUpdate} - Executing event: at frame {CurrentKeyFrameIndex}");
                    _morphEventsToExecuteThisUpdate++;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// This method solves multiple circumstances:
        /// (1). Gothic animations won't always start from frame 0. e.g. t_Potion_Random_1 expects to work from frame 45+.
        ///      --> This might be, as the animations are "behind" another and could be one single animation in Gothic.
        ///      --> But in GUZ, we create every transition animation separately and therefore normalize to start from frame 0.
        /// (2). G1 animation key frames are optimized and not always aligned with 25fps (e.g. t_Potion_* leverages 10 frames only).
        ///      But the animation event frame numbers are matching 25fps.
        ///      --> In Unity we only store the key frames and fps value provided (e.g. 10fps), as Unity will interpolate on it's own.
        ///      --> But then we need to calculate the ratio between the fpsSource (G1=25fps) and the actual fps (e.g. 10fps).
        /// (3). Some animation events seem to be executed before or after the actual animation.
        ///      --> We take care by checking its boundaries.
        /// </summary>
        private float ClampFrame(int expectedFrame)
        {
            // (2). calculate ration between FpsSource and the animations Fps.
            var animationRatio = Track.ModelAnimation.Fps / Track.ModelAnimation.FpsSource;

            // (1). Norm to start frame of 1
            // (2). Norm to fpsSource (==25 in G1)
            expectedFrame = (int)Math.Round((expectedFrame - Track.Animation.FirstFrame) * animationRatio);

            // (3). check for misaligned animation frame boundaries (if any).
            if (expectedFrame < 0)
            {
                return 0;
            }

            if (expectedFrame >= Track.ModelAnimation.FrameCount)
            {
                return Track.ModelAnimation.FrameCount - 1;
            }

            return expectedFrame;
        }

        /// <summary>
        /// Apply BlendWeight changes to each bone
        /// </summary>
        private void UpdateBoneWeights(float deltaTime)
        {
            for (var i = 0; i < Track.BoneCount; i++)
            {
                switch (BoneStates[i])
                {
                    case AnimationState.BlendIn:
                        BoneBlendWeights[i] += deltaTime / BoneBlendTimes[i];

                        if (BoneBlendWeights[i] >= 1f)
                        {
                            BoneBlendWeights[i] = 1f;
                            BoneStates[i] = AnimationState.Play;
                            BoneAmountStatePlay++;
                        }
                        break;
                    case AnimationState.BlendOut:
                        if (BoneStates[i] == AnimationState.BlendOut)
                        {
                            BoneBlendWeights[i] -= deltaTime / BoneBlendTimes[i];

                            if (BoneBlendWeights[i] <= 0f)
                            {
                                BoneBlendWeights[i] = 0f;
                                BoneStates[i] = AnimationState.Stop;
                                BoneAmountStateStop++;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Update main state of Animation. If nothing changed, we return AnimationState.None.
        /// </summary>
        private AnimationState UpdateState()
        {
            // Update State if blending is done for all bones.
            switch (State)
            {
                case AnimationState.BlendIn:
                    if (BoneAmountStatePlay >= Track.BoneCount)
                    {
                        BoneAmountStatePlay = Track.BoneCount;
                        State = AnimationState.Play;
                        return AnimationState.Play;
                    }
                    break;
                case AnimationState.Play:
                    break;
                case AnimationState.BlendOut:
                    if (BoneAmountStateStop >= Track.BoneCount)
                    {
                        BoneAmountStateStop = Track.BoneCount;
                        State = AnimationState.Stop;
                        return AnimationState.Stop;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return AnimationState.None;
        }

        public void GetBonePose(int boneIndex, out Vector3 position, out Quaternion rotation)
        {
            Track.GetBonePose(boneIndex, CurrentKeyFrameIndex, out position, out rotation);
        }

        /// <summary>
        /// About BlendOut times:
        /// a.) the new animation has higher or same Layer, then its BlendIn time is used as our BlendOut time.
        /// b.) there is no new animation, then we use our BlendOut time.
        /// </summary>
        public void BlendOutTrack(float blendOutTime)
        {
            State = AnimationState.BlendOut;

            for (var i = 0; i < Track.BoneCount; i++)
            {
                switch (BoneStates[i])
                {
                    case AnimationState.BlendIn:
                    case AnimationState.Play:
                        BoneBlendTimes[i] = blendOutTime;
                        BoneStates[i] = AnimationState.BlendOut;
                        break;
                    case AnimationState.BlendOut:
                    case AnimationState.Stop:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void BlendOutBones(string[] boneNames, float blendOutTime)
        {
            // Skip partial BlendOut if we're already blending out.
            if (State == AnimationState.BlendOut)
            {
                return;
            }

            for (var i = 0; i < boneNames.Length; i++)
            {
                var boneName = boneNames[i];
                var boneIndex = Track.BoneNames.IndexOfItem(boneName);

                if (boneIndex == -1)
                {
                    continue;
                }

                if (BoneStates[boneIndex] == AnimationState.Play)
                {
                    BoneAmountStatePlay--;
                }
                BoneStates[boneIndex] = AnimationState.BlendOut;
                BoneBlendTimes[boneIndex] = blendOutTime;
            }
        }

        public void BlendInBones(string[] boneNames, float blendInTime)
        {
            // Skip partial BlendIn if we're already blending in.
            if (State == AnimationState.BlendIn)
            {
                return;
            }

            for (var i = 0; i < boneNames.Length; i++)
            {
                var boneName = boneNames[i];
                var boneIndex = Track.BoneNames.IndexOfItem(boneName);

                if (boneIndex == -1)
                {
                    continue;
                }

                if (BoneStates[boneIndex] == AnimationState.Stop)
                {
                    BoneAmountStateStop--;
                }

                BoneStates[boneIndex] = AnimationState.BlendIn;
                BoneBlendTimes[boneIndex] = blendInTime;
            }
        }

        public int GetBoneIndex(string boneName)
        {
            return Track.BoneNames.IndexOfItem(boneName);
        }

        [CanBeNull]
        public List<IEventTag> GetPendingEventTags()
        {
            if (_animationEventsToExecuteThisUpdate == 0)
            {
                return null;
            }

            return Track.Animation.EventTags.GetRange(_lastExecutedAnimationEvent + 1, _animationEventsToExecuteThisUpdate);
        }

        public List<IEventParticleEffect> GetPendingParticleEffects()
        {
            if (_pfxEventsToExecuteThisUpdate == 0)
            {
                return null;
            }

            return Track.Animation.ParticleEffects.GetRange(_lastExecutedPfxEvent + 1, _pfxEventsToExecuteThisUpdate);
        }

        public List<IEventMorphAnimation> GetPendingMorphAnimations()
        {
            if (_morphEventsToExecuteThisUpdate == 0)
            {
                return null;
            }

            return Track.Animation.MorphAnimations.GetRange(_lastExecutedMorphEvent + 1, _morphEventsToExecuteThisUpdate);
        }

        public List<IEventSoundEffect> GetPendingSoundEffects()
        {
            if (_sfxEventsToExecuteThisUpdate == 0)
            {
                return null;
            }

            return Track.Animation.SoundEffects.GetRange(_lastExecutedSfxEvent + 1, _sfxEventsToExecuteThisUpdate);
        }
    }
}
