using System;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Animations
{
    public class AnimationTrack
    {
        public string[] BoneNames;
        public int BoneCount;
        public int FrameCount;
        public AnimationKeyFrame[] KeyFrames;
        public IAnimation Animation;
        public IModelAnimation ModelAnimation;
        public int Layer;
        public float Duration;
        public float FrameTime;

        public bool IsMoving;
        public Vector3 MovementSpeed;


        public void GetBonePose(int boneIndex, int frameIndex, out Vector3 position, out Quaternion rotation)
        {
            var keyFrame = KeyFrames[frameIndex * BoneCount + boneIndex];

            position = keyFrame.Position;
            rotation = keyFrame.Rotation;
        }
    }
}
