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

        public static Dictionary<string, AnimationTrack> Tracks = new();

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
            track.Duration = CalculateDuration(track);

            Tracks.Add(name, track);

            return track;
        }

        public static AnimationTrack CreateTrack(IModelAnimation modelAnimation,
            IModelHierarchy modelHierarchy, IAnimation anim)
        {
            var track = new AnimationTrack()
            {
                Animation = anim
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

            return track;
        }

        private static float CalculateDuration(AnimationTrack track)
        {
            return track.Animation.LastFrame - track.Animation.FirstFrame / track.Animation.Fps;
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
