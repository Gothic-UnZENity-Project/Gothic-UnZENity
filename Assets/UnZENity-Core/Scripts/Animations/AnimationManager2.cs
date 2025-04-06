using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using JetBrains.Annotations;
using UnityEditor.Search;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Animations
{
    public class AnimationManager2
    {
        private const float _movementThreshold = 0.4f; // If magnitude of first and last frame positions is higher than this, we have a movement animation.

        [ItemCanBeNull]
        private static Dictionary<string, AnimationTrack> _tracks = new();


        /// <summary>
        /// Try to load animation based on both MDS values: overlay + base.
        /// </summary>
        public static AnimationTrack GetTrack(string animName, string mdsBase, string mdsOverlay)
        {
            var track = GetTrack(animName, mdsOverlay);
            if (track == null)
            {
                track = GetTrack(animName, mdsBase);
            }
            return track;
        }

        private static AnimationTrack GetTrack(string animName, string mdsName)
        {
            if (mdsName == null)
            {
                return null;
            }

            var name = GetCombinedAnimationKey(mdsName, animName);

            if (_tracks.TryGetValue(name, out var track))
            {
                return track;
            }

            var mds = ResourceLoader.TryGetModelScript(mdsName)!;

            var anim = mds.Animations.FirstOrDefault(i => i.Name.EqualsIgnoreCase(animName));
            IAnimationAlias animAlias = null;

            // AniAlias lookup
            // Looking up multiple animation types (Animation/AniAlias) for the actual animation name.
            // HINT: This also means, that we potentially create a Track based on duplicate animation data for AniAlias.
            //       As we have no memory issue, this is neglectable for now.
            if (anim == null)
            {
                // FIXME - Alias values aren't overwritten as of today, they need to be handled:
                //         aniAlias (ANI_NAME LAYER NEXT_ANI BLEND_IN BLEND_OUT FLAGS ALIAS_NAME ANI_DIR)
                animAlias = mds.AnimationAliases.FirstOrDefault(i => i.Name.EqualsIgnoreCase(animName));
                if (animAlias != null)
                {
                    anim = mds.Animations.FirstOrDefault(i => i.Name.EqualsIgnoreCase(animAlias.Alias));
                }
            }

            // Nothing found
            if (anim == null)
            {
                // Caching an empty track means, we don't need to try creating this track again.
                _tracks.Add(name, null);
                return null;
            }
            animName = anim.Name; // We need to use the actual name, as we might have an alias.

            var modelAnimation = ResourceLoader.TryGetModelAnimation(mdsName, animName);
            if (modelAnimation == null)
            {
                // Caching an empty track means, we don't need to try creating this track again.
                _tracks.Add(name, null);
                return null;
            }

            // For animations: mdhName == mdsName (with different file ending of course ;-))
            var mdhName = mdsName;
            var mdh = ResourceLoader.TryGetModelHierarchy(mdhName);

            track = CreateTrack(modelAnimation, mdh, anim, animAlias);
            AddTrackDuration(track, modelAnimation);
            SetClipMovementSpeed(track, modelAnimation, mdh);

            _tracks.Add(name, track);

            return track;
        }

        private static AnimationTrack CreateTrack(IModelAnimation modelAnimation,
            IModelHierarchy modelHierarchy, IAnimation anim, IAnimationAlias animAlias)
        {
            var track = new AnimationTrack(anim, animAlias, modelAnimation);

            // Get bone names from model hierarchy using node indices
            track.BoneNames = modelAnimation.NodeIndices
                .Select(nodeIndex => modelHierarchy.Nodes[nodeIndex].Name)
                .ToArray();

            // Process animation samples
            track.BoneCount = modelAnimation.NodeCount;
            track.FrameCount = modelAnimation.FrameCount;
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
                    if (boneName == Constants.Animations.RootBoneName)
                    {
                        track.KeyFrames[sampleIndex].Position = Vector3.zero;
                    }
                }
            }

            if (track.Flags.HasFlag(AnimationFlags.Rotate))
            {
                Debug.LogWarning($"{track.Name}: Rotation animations are not supported yet.");
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
        ///
        /// We don't use Flags.Move as also idle animations would move the characters (based on animation data).
        /// Using our own calculation is a workaround found on OpenGothic.
        /// </summary>
        private static void SetClipMovementSpeed(AnimationTrack track, IModelAnimation modelAnim, IModelHierarchy mdh)
        {
            // We assume, that only lowest level animations are movement animations. (e.g. S_WALKL)
            if (track.Layer != 1)
            {
                return;
            }

            var firstBoneIndex = modelAnim.NodeIndices.First();
            var isRootBoneExisting = mdh.Nodes[firstBoneIndex].Name == Constants.Animations.RootBoneName;

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
