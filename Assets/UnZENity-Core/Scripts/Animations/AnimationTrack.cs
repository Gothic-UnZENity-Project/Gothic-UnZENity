using System;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Animations
{
    public class AnimationTrack
    {
        public string[] BoneNames;
        public int BoneCount;
        public AnimationKeyFrame[] KeyFrames;
        public IAnimation Animation;
        public IModelAnimation ModelAnimation;
        public int Layer;
        public float Duration;
        public float FrameTime;

        public bool TryGetBonePose(string boneName, int frameIndex, out Vector3 position, out Quaternion rotation, out int boneIndex)
        {
            for (boneIndex = 0; boneIndex < BoneNames.Length; boneIndex++)
            {
                if (BoneNames[boneIndex] != boneName)
                {
                    continue;
                }

                var keyFrame = KeyFrames[frameIndex * BoneCount + boneIndex];

                position = keyFrame.Position;
                rotation = keyFrame.Rotation;
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }
    }
}
