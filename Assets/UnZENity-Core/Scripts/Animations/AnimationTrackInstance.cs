using System;
using System.Collections.Generic;
using PlasticGui.WorkspaceWindow.Locks;
using UnityEngine;
using Transform = log4net.Util.Transform;

namespace GUZ.Core.Animations
{
    /// <summary>
    /// Currently playing instance of a Track on a NPC.
    /// </summary>
    public class AnimationTrackInstance
    {
        public AnimationTrack Track;

        // Value for this specific point in time
        public float CurrentBlendWeight;
        public float CurrentTime;
        public int CurrentKeyFrameIndex;
        public float NextKeyframeTime;
        public float BlendOutTime;
        public AnimationState State;

        public AnimationTrackInstance(AnimationTrack track)
        {
            Track = track;
            CurrentBlendWeight = 0f;
            CurrentTime = 0f;
            CurrentKeyFrameIndex = 0;
            NextKeyframeTime = track.FrameTime;
            BlendOutTime = 0f; // Not used until AnimationState.BlendOut is started.
            State = AnimationState.BlendIn;
        }

        public void Update(float deltaTime)
        {
            CurrentTime += deltaTime;
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

            switch (State)
            {
                case AnimationState.BlendIn:
                    CurrentBlendWeight += deltaTime / Track.Animation.BlendIn;
                    CurrentBlendWeight = Mathf.Min(CurrentBlendWeight, 1f);

                    if (CurrentBlendWeight >= 1f)
                    {
                        State = AnimationState.Playing;
                    }
                    break;
                case AnimationState.Playing:
                    break;
                case AnimationState.BlendOut:
                    CurrentBlendWeight -= deltaTime / BlendOutTime;
                    CurrentBlendWeight = Mathf.Max(CurrentBlendWeight, 0f);

                    if (CurrentBlendWeight <= 0f)
                    {
                        State = AnimationState.Stopped;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool TryGetBonePose(string boneName, out Vector3 position, out Quaternion rotation)
        {
            if (!Track.TryGetBonePose(boneName, CurrentKeyFrameIndex, out position, out rotation))
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            // Apply blending weight
            position *= CurrentBlendWeight;
            rotation = Quaternion.Slerp(Quaternion.identity, rotation, CurrentBlendWeight);
            return true;
        }

        // FIXME - I'm not sure, if we need to use own animation's BlendOut time or the BlendIn time for the other (new) animation.
        public void BlendOutTrack(float blendOutTime)
        {
            BlendOutTime = blendOutTime;
            State = AnimationState.BlendOut;
        }
    }
}
