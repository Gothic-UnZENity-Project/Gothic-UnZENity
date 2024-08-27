using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Npc;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Properties;
using MyBox;
using UnityEngine;
using ZenKit;
using Animation = UnityEngine.Animation;

namespace GUZ.Core.Creator
{
    public static class AnimationCreator
    {
        /// <summary>
        /// Handling animations for baseMds and overlayMds
        /// </summary>
        public static bool PlayAnimation(string[] mdsNames, string animationName, GameObject go, bool repeat = false)
        {
            // We assume, that we get mdsNames in this order: base, overlay. But we should always check for overlay first.
            foreach (var mdsName in mdsNames.Reverse())
            {
                if (TryPlayAnimation(mdsName, animationName, go, repeat))
                {
                    return true;
                }
            }

            // No suitable animation found.
            return false;
        }

        private static bool TryPlayAnimation(string mdsName, string animationName, GameObject go, bool repeat)
        {
            // For animations: mdhName == mdsName (with different file ending of course ;-))
            var mdhName = mdsName;

            var modelAnimation = ResourceLoader.TryGetModelAnimation(mdsName, animationName);
            if (modelAnimation == null)
            {
                return false;
            }

            var mdsAnimationKeyName = GetCombinedAnimationKey(mdsName, animationName);
            var animationComp = go.GetComponent<Animation>();

            var mds = ResourceLoader.TryGetModelScript(mdsName);
            var mdh = ResourceLoader.TryGetModelHierarchy(mdhName);
            var anim = mds.Animations.First(i => i.Name.EqualsIgnoreCase(animationName));

            // If we create empty animations with only one frame, Unity will complain. We therefore skip it for now.
            if (anim.FirstFrame == anim.LastFrame)
            {
                return false;
            }

            if (anim.Direction == AnimationDirection.Backward)
            {
                Debug.LogWarning(
                    $"Backwards animations not yet handled. Called for >{animationName}< from >{mdsName}<. Currently playing Forward.");
            }

            // Try to load from cache
            if (!LookupCache.AnimationClipCache.TryGetValue(mdsAnimationKeyName, out var clip))
            {
                clip = LoadAnimationClip(modelAnimation, mdh, go, repeat, mdsAnimationKeyName);
                LookupCache.AnimationClipCache[mdsAnimationKeyName] = clip;

                AddClipEvents(clip, modelAnimation, anim);
                AddClipEndEvent(anim, clip);
            }

            if (animationComp[mdsAnimationKeyName] == null)
            {
                animationComp.AddClip(clip, mdsAnimationKeyName);
                animationComp[mdsAnimationKeyName]!.layer = modelAnimation.Layer;
            }

            animationComp.Stop();
            animationComp.Play(mdsAnimationKeyName);

            return true;
        }

        public static void StopAnimation(GameObject go)
        {
            var animationComp = go.GetComponent<Animation>();

            // Rewind workaround to actually set NPC to first frame of the animation.
            // @see: https://forum.unity.com/threads/animation-rewind-not-working.4756/
            if (!animationComp.isPlaying)
            {
                return;
            }

            animationComp.Rewind();
            animationComp.Play();
            animationComp.Sample();
            animationComp.Stop();
        }

        /// <summary>
        /// Blends the specified animation on the given GameObject.
        /// <br/>
        /// Note on excludeBones: <b>Currently this is only used to detach Head so it's not affected by animation while rotating (LookAt method)</b>
        /// </summary>
        /// <param name="mdsNames">An array of MDS file names to search for the animation.</param>
        /// <param name="animationName">The name of the animation to blend.</param>
        /// <param name="go">The GameObject to blend the animation on.</param>
        /// <param name="repeat">Whether the animation should repeat or not.</param>
        /// <param name="excludeBones">A list of bone names to exclude from the loaded animation. <b>Currently this is only used to detach Head so it's not affected by animation while rotating (LookAt)</b></param>
        /// <returns>True if the animation was successfully blended, false otherwise.</returns>
        public static bool BlendAnimation(string[] mdsNames, string animationName, GameObject go, bool repeat = false, List<string> excludeBones = null)
        {
            foreach (var mdsName in mdsNames.Reverse())
            {
                if (TryBlendAnimation(mdsName, animationName, go, repeat, excludeBones))
                {
                    return true;
                }
            }

            // No suitable animation found.
            return false;
        }

        public static bool TryBlendAnimation(string mdsName, string animationName, GameObject go, bool repeat, List<string> excludeBones = null)
        {
            // For animations: mdhName == mdsName (with different file ending of course ;-))
            var mdhName = mdsName;

            var modelAnimation = ResourceLoader.TryGetModelAnimation(mdsName, animationName);
            var mds = ResourceLoader.TryGetModelScript(mdsName);
            if (modelAnimation == null)
            {
                Debug.Log("BoroLog: ERROR Couldn't find " + animationName + " animation");
                return false;
            }

            var mdsAnimationKeyName = GetCombinedAnimationKey(mdsName, animationName);
            var animationComp = go.GetComponent<Animation>();

            var mdh = ResourceLoader.TryGetModelHierarchy(mdhName);
            var anim = mds.Animations.First(i => i.Name.EqualsIgnoreCase(animationName));

            // If we create empty animations with only one frame, Unity will complain. We therefore skip it for now.
            if (anim.FirstFrame == anim.LastFrame)
            {
                Debug.Log("BoroLog: ERROR first frame == last frame");
                return false;
            }

            if (anim.Direction == AnimationDirection.Backward)
            {
                Debug.LogWarning(
                    $"Backwards animations not yet handled. Called for >{animationName}< from >{mdsName}<. Currently playing Forward.");
            }

            // Try to load from cache
            if (!LookupCache.AnimationClipCache.TryGetValue(mdsAnimationKeyName, out var clip) || !excludeBones.IsNullOrEmpty())
            {
                clip = LoadAnimationClip(modelAnimation, mdh, go, repeat, mdsAnimationKeyName, excludeBones);
                LookupCache.AnimationClipCache[mdsAnimationKeyName] = clip;

                AddClipEvents(clip, modelAnimation, anim);
                AddClipEndEvent(anim, clip);
            }

            if (animationComp[mdsAnimationKeyName] == null)
            {
                animationComp.AddClip(clip, mdsAnimationKeyName);
                animationComp[mdsAnimationKeyName]!.layer = modelAnimation.Layer;
            }

            animationComp.Blend(mdsAnimationKeyName);

            return true;
        }

        public static void PlayHeadMorphAnimation(NpcProperties props, HeadMorph.HeadMorphType type)
        {
            props.HeadMorph.StartAnimation(props.BodyData.Head, type);
        }

        public static void StopHeadMorphAnimation(NpcProperties props, HeadMorph.HeadMorphType type)
        {
            props.HeadMorph.StopAnimation(type);
        }

        private static AnimationClip LoadAnimationClip(IModelAnimation pxAnimation, IModelHierarchy mdh,
            GameObject rootBone, bool repeat, string clipName, List<string> excludeBones = null)
        {
            var clip = new AnimationClip
            {
                legacy = true,
                name = clipName,
                wrapMode = repeat ? WrapMode.Loop : WrapMode.Once
            };

            var curves = new Dictionary<string, List<AnimationCurve>>(pxAnimation.NodeCount);
            var boneNames = pxAnimation.NodeIndices.Select(nodeIndex => mdh.Nodes[nodeIndex].Name).ToArray();

            // Initialize array
            for (var boneId = 0; boneId < boneNames.Length; boneId++)
            {
                var boneName = boneNames[boneId];

                // Skip adding curves for excluded bones
                if (excludeBones != null && excludeBones.Contains(boneName))
                {
                    continue;
                }

                curves.Add(boneName, new List<AnimationCurve>(7));

                // Initialize 7 dimensions. (3x position, 4x rotation)
                curves[boneName].AddRange(Enumerable.Range(0, 7).Select(i => new AnimationCurve()).ToArray());
            }

            var rootBoneStartCorrection = Vector3.zero;

            // Add KeyFrames from PxSamples
            for (var i = 0; i < pxAnimation.Samples.Count; i++)
            {
                // We want to know what time it is for the animation.
                // Therefore we need to know fps multiplied with current sample. As there are nodeCount samples before a new time starts,
                // we need to add this to the calculation.
                var time = 1 / pxAnimation.Fps * (i / pxAnimation.NodeCount);
                var sample = pxAnimation.Samples[i];
                var boneId = i % pxAnimation.NodeCount;
                var boneName = boneNames[boneId];

                if (excludeBones != null && excludeBones.Contains(boneName))
                {
                    continue;
                }

                var boneList = curves[boneName];
                var isRootBone = boneName.EqualsIgnoreCase("BIP01");

                // Some animations don't start with BIP01=(0,0,0).
                // Therefore we need to calculate the offset.
                // Otherwise e.g. walking will hick up as NPC will _spawn_ slightly in front of last animation loop.
                if (time == 0.0f && isRootBone)
                {
                    rootBoneStartCorrection = sample.Position.ToUnityVector();
                }

                Vector3 uPosition;
                if (isRootBone)
                {
                    uPosition = sample.Position.ToUnityVector() - rootBoneStartCorrection;
                }
                else
                {
                    uPosition = sample.Position.ToUnityVector();
                }

                // We add 7 properties for location and rotation.
                boneList[0].AddKey(time, uPosition.x);
                boneList[1].AddKey(time, uPosition.y);
                boneList[2].AddKey(time, uPosition.z);

                // It's important to have this value with a -1. Otherwise animation is inversed.
                boneList[3].AddKey(time, -sample.Rotation.W);
                boneList[4].AddKey(time, sample.Rotation.X);
                boneList[5].AddKey(time, sample.Rotation.Y);
                boneList[6].AddKey(time, sample.Rotation.Z);
            }

            foreach (var entry in curves)
            {
                var path = GetChildPathRecursively(rootBone.transform, entry.Key, "");

                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", entry.Value[0]);
                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", entry.Value[1]);
                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", entry.Value[2]);
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", entry.Value[3]);
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", entry.Value[4]);
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", entry.Value[5]);
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", entry.Value[6]);
            }

            // Add some final settings
            clip.EnsureQuaternionContinuity();
            clip.frameRate = pxAnimation.Fps;

            return clip;
        }

        // TODO - If we have a performance bottleneck while loading animations, then we could cache these results.
        private static string GetChildPathRecursively(Transform parent, string curName, string currentPath)
        {
            var result = parent.Find(curName);

            if (result != null)
            {
                // The child object was found, return the current path
                if (currentPath != "")
                {
                    return currentPath + "/" + curName;
                }

                return curName;
            }

            // Search recursively in the children of the current object
            foreach (Transform child in parent)
            {
                var childPath = currentPath + "/" + child.name;
                var resultPath = GetChildPathRecursively(child, curName, childPath);

                // The child object was found in a recursive call, return the result path
                if (resultPath != null)
                {
                    return resultPath.TrimStart('/');
                }
            }

            // The child object was not found
            return null;
        }

        private static void AddClipEvents(AnimationClip clip, IModelAnimation modelAnimation, IAnimation anim)
        {
            foreach (var zkEvent in anim.EventTags)
            {
                var clampedFrame = ClampFrame(zkEvent.Frame, modelAnimation, anim);

                AnimationEvent animEvent = new()
                {
                    time = clampedFrame / clip.frameRate,
                    functionName = nameof(IAnimationCallbacks.AnimationCallback),

                    // As we can't add a custom object, we serialize the data object.
                    stringParameter = JsonUtility.ToJson(new SerializableEventTag(zkEvent))
                };

                clip.AddEvent(animEvent);
            }

            foreach (var sfxEvent in anim.SoundEffects)
            {
                var clampedFrame = ClampFrame(sfxEvent.Frame, modelAnimation, anim);
                AnimationEvent animEvent = new()
                {
                    time = clampedFrame / clip.frameRate,
                    functionName = nameof(IAnimationCallbacks.AnimationSfxCallback),

                    // As we can't add a custom object, we serialize the data object.
                    stringParameter = JsonUtility.ToJson(new SerializableEventSoundEffect(sfxEvent))
                };

                clip.AddEvent(animEvent);
            }

            foreach (var morphEvent in anim.MorphAnimations)
            {
                var clampedFrame = ClampFrame(morphEvent.Frame, modelAnimation, anim);
                AnimationEvent animEvent = new()
                {
                    time = clampedFrame / clip.frameRate,
                    functionName = nameof(IAnimationCallbacks.AnimationMorphCallback),

                    // As we can't add a custom object, we serialize the data object.
                    stringParameter = JsonUtility.ToJson(new SerializableEventMorphAnimation(morphEvent))
                };

                clip.AddEvent(animEvent);
            }

            if (anim.ParticleEffects.Any())
            {
                Debug.LogWarning($"SFX events not yet implemented. Tried to use for {anim.Name}");
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
        private static float ClampFrame(int expectedFrame, IModelAnimation modelAnimation, IAnimation anim)
        {
            // (2). calculate ration between FpsSource and the animations Fps.
            var animationRatio = modelAnimation.Fps / modelAnimation.FpsSource;

            // (1). Norm to start frame of 1
            // (2). Norm to fpsSource (==25 in G1)
            expectedFrame = (int)Math.Round((expectedFrame - anim.FirstFrame) * animationRatio);

            // (3). check for misaligned animation frame boundaries (if any).
            if (expectedFrame < 0)
            {
                return 0;
            }

            if (expectedFrame >= modelAnimation.FrameCount)
            {
                return modelAnimation.FrameCount - 1;
            }

            return expectedFrame;
        }

        /// <summary>
        /// Adds event at the end of animation.
        /// The event is called on every MonoBehaviour on GameObject where Clip is played.
        /// @see: https://docs.unity3d.com/ScriptReference/AnimationEvent.html
        /// @see: https://docs.unity3d.com/ScriptReference/GameObject.SendMessage.html
        /// </summary>
        private static void AddClipEndEvent(IAnimation anim, AnimationClip clip)
        {
            AnimationEvent finalEvent = new()
            {
                time = clip.length,
                functionName = nameof(IAnimationCallbacks.AnimationEndCallback),
                stringParameter = JsonUtility.ToJson(new SerializableEventEndSignal(anim.Next))
            };

            clip.AddEvent(finalEvent);
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
