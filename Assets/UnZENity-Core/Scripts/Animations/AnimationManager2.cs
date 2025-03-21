using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core.Extensions;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Animations
{
    public class AnimationManager2
    {
        private const string _rootBoneName = "BIP01";
        private const float _movementThreshold = 0.4f; // If magnitude of first and last frame positions is higher than this, we have a movement animation.

        private static Dictionary<string, AnimationTrack> Tracks = new();

        public static AnimationTrack GetTrack(string animName, string mdsBase, string mdsOverlay)
        {
            var name = GetCombinedAnimationKey(mdsBase, animName);

            if (Tracks.TryGetValue(name, out var track))
            {
                return track;
            }

            var modelAnimation = ResourceLoader.TryGetModelAnimation(mdsBase, animName);
            if (modelAnimation == null)
            {
                return null;
            }

            // For animations: mdhName == mdsName (with different file ending of course ;-))
            var mdhName = mdsBase;
            var mdh = ResourceLoader.TryGetModelHierarchy(mdhName);
            var mds = ResourceLoader.TryGetModelScript(mdsBase)!;
            var anim = mds.Animations.First(i => i.Name.EqualsIgnoreCase(animName));

            track = CreateTrack(modelAnimation, mdh, anim);
            AddTrackDuration(track, modelAnimation);
            SetClipMovementSpeed(track, modelAnimation, mdh);

            Tracks.Add(name, track);

            return track;
        }

        private static AnimationTrack CreateTrack(IModelAnimation modelAnimation,
            IModelHierarchy modelHierarchy, IAnimation anim)
        {
            var track = new AnimationTrack
            {
                Animation = anim,
                ModelAnimation = modelAnimation,
                Layer = anim.Layer
            };

            // Get bone names from model hierarchy using node indices
            track.BoneNames = modelAnimation.NodeIndices
                .Select(nodeIndex => modelHierarchy.Nodes[nodeIndex].Name)
                .ToArray();

            // Process animation samples
            track.BoneCount = modelAnimation.NodeCount;
            track.KeyFrames = new AnimationKeyFrame[modelAnimation.Samples.Count];
            track.FrameTime = 1 / modelAnimation.Fps;

            for (var frameIndex = 0; frameIndex < modelAnimation.Samples.Count / track.BoneCount; frameIndex++)
            {
                for (var nodeIndex = 0; nodeIndex < track.BoneCount; nodeIndex++)
                {
                    var sampleIndex = frameIndex * track.BoneCount + nodeIndex;
                    var sample = modelAnimation.Samples[sampleIndex];
                    var boneName = track.BoneNames[nodeIndex];

                    track.KeyFrames[sampleIndex] = new AnimationKeyFrame
                    {
                        Position = sample.Position.ToUnityVector(),
                        Rotation = new Quaternion(
                            sample.Rotation.X,
                            sample.Rotation.Y,
                            sample.Rotation.Z,
                            -sample.Rotation.W) // Note: W is negated as per original code
                    };

                    // Special handling for root bone (BIP01)
                    if (boneName == _rootBoneName)
                    {
                        track.KeyFrames[sampleIndex].Position = Vector3.zero;
                    }
                }
            }

            if (track.Animation.Flags.HasFlag(AnimationFlags.Rotate))
            {
                Debug.LogWarning($"{track.Animation.Name}: Rotation animations are not supported yet.");
            }

            return track;
        }

        private static void AddTrackDuration(AnimationTrack track, IModelAnimation modelAnimation)
        {
            track.Duration = modelAnimation.FrameCount / modelAnimation.Fps;
        }

        /// <summary>
        /// Based on first node (BIP01), we calculate its start position and end position of the animation.
        /// If it's above a threshold, we have a movement animation.
        /// </summary>
        private static void SetClipMovementSpeed(AnimationTrack track, IModelAnimation modelAnim, IModelHierarchy mdh)
        {
            // We assume, that only lowest level animations are movement animations. (e.g. S_WALKL)
            if (track.Layer != 1)
            {
                return;
            }

            var firstBoneIndex = modelAnim.NodeIndices.First();
            var isRootBoneExisting = mdh.Nodes[firstBoneIndex].Name == _rootBoneName;

            // I don't think it will ever happen, but better safe than sorry.
            if (!isRootBoneExisting)
            {
                return;
            }

            var boneCount = modelAnim.NodeCount;
            var firstSample = modelAnim.Samples[0];
            var lastSample = modelAnim.Samples[modelAnim.SampleCount - boneCount];

            var movement = (lastSample.Position - firstSample.Position).ToUnityVector();

            if (movement.sqrMagnitude < _movementThreshold)
            {
                return;
            }

            track.IsMoving = true;

            // TODO - We can also check if we do a "movement" calculation based on each frame. Then animations might "woggle" during walk instead of walking on a rubber band.
            track.MovementSpeed = movement * (modelAnim.FrameCount / modelAnim.Fps);
        }

        /// <summary>
        /// .man files are combined of MDSNAME-ANIMATIONNAME.man
        /// </summary>
        private static string GetCombinedAnimationKey(string mdsKey, string animKey)
        {
            var preparedMdsKey = GetPreparedKey(mdsKey);
            var preparedAnimKey = GetPreparedKey(animKey);

            return preparedMdsKey + "-" + preparedAnimKey;
        }

        /// <summary>
        /// Basically extract file ending and lower names.
        /// </summary>
        private static string GetPreparedKey(string key)
        {
            var lowerKey = key.ToLower();
            var extension = Path.GetExtension(lowerKey);

            if (extension == string.Empty)
            {
                return lowerKey;
            }

            return lowerKey.Replace(extension, "");
        }
    }
}
