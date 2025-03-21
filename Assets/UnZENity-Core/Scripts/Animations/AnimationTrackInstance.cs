using System;
using MyBox;
using UnityEngine;

namespace GUZ.Core.Animations
{
    /// <summary>
    /// Currently playing instance of a Track on a NPC.
    /// </summary>
    public class AnimationTrackInstance
    {
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

        public float BlendOutStart;
        public bool IsLooping;

        public AnimationTrackInstance(AnimationTrack track)
        {
            Track = track;
            State = AnimationState.BlendIn;
            CurrentTime = 0f;
            CurrentKeyFrameIndex = 0;
            NextKeyframeTime = track.FrameTime;

            IsLooping = track.Animation.Name == track.Animation.Next;
            if (!IsLooping)
            {
                BlendOutStart = track.Duration - track.Animation.BlendOut;
            }

            BoneStates = new AnimationState[Track.BoneCount];
            BoneBlendWeights = new float[Track.BoneCount];
            BoneBlendTimes = new float[Track.BoneCount];
            for (var i = 0; i < Track.BoneCount; i++)
            {
                BoneStates[i] = AnimationState.BlendIn;
                BoneBlendWeights[i] = 0f;
                BoneBlendTimes[i] = Track.Animation.BlendIn;
            }
            BoneAmountStatePlay = 0;
            BoneAmountStateStop = 0;
        }

        /// <summary>
        /// Update animation information at each frame.
        /// Return status change of whole animation if happening now. (e.g. BlendOut).
        /// </summary>
        public AnimationState Update(float deltaTime)
        {
            CurrentTime += deltaTime;

            if (!IsLooping && State == AnimationState.Play && CurrentTime >= BlendOutStart)
            {
                BlendOutTrack(Track.Animation.BlendOut);
                return AnimationState.BlendOut;
            }

            if (CurrentTime >= Track.Duration)
            {
                CurrentTime %= Track.Duration;
                CurrentKeyFrameIndex = 0;
                NextKeyframeTime = CurrentKeyFrameIndex * Track.FrameTime;
            }

            if (CurrentTime >= NextKeyframeTime)
            {
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


            // Apply BlendWeight changes to each bone
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

            // Apply blending weight
            var weight = BoneBlendWeights[boneIndex];
            position *= weight;
            rotation = Quaternion.Slerp(Quaternion.identity, rotation, weight);
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
                BoneBlendWeights[boneIndex] = 1f;
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
                BoneBlendWeights[boneIndex] = 0f;
                BoneBlendTimes[boneIndex] = blendInTime;
            }
        }

        /// <summary>
        /// Immediately start animation with full bone weight.
        /// </summary>
        public void SetPlayState()
        {
            State = AnimationState.Play;
            BoneAmountStatePlay = Track.BoneCount;
            BoneAmountStateStop = 0;
            for (var i = 0; i < Track.BoneCount; i++)
            {
                BoneStates[i] = AnimationState.Play;
                BoneBlendWeights[i] = 1f;
            }
        }

        public int GetBoneIndex(string boneName)
        {
            return Track.BoneNames.IndexOfItem(boneName);
        }
    }
}
