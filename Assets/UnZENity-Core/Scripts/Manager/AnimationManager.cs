using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Npc.Actions;
using JetBrains.Annotations;
using MyBox;
using UnityEngine;
using ZenKit;
using Animation = UnityEngine.Animation;

namespace GUZ.Core.Manager
{
    public class AnimationManager
    {
        /// <summary>
        /// Handling animations for baseMds and overlayMds.
        ///
        /// We always apply BlendIn. If there is another animation playing right now, it will be CrossFaded.
        /// If not, it will be started with weight=1 immediately (Unity behavior).
        /// BlendOut will be applied by the callee via Coroutine as it's dynamic based on upcoming animation.
        /// </summary>
        public bool PlayAnimation(Animation animComp, string[] mdsNames, string animName)
        {
            var anim = GetCachedAnimationData(mdsNames, animName, animComp, out var combinedAnimationName);

            if (anim == null)
            {
                return false;
            }

            // TODO - When calculating BlendOut of previous animation, we say anim1.BlendOut - anim2.BlendIn. Here we only say anim2.BlendIn.
            // TODO - This makes the Fade (e.g.) twice as fast. Okay for now, but could be improved. @see: NpcAnimationHandler.BlendOutCoroutine()
            animComp.CrossFade(combinedAnimationName, anim.BlendIn);
            return true;
        }

        public float GetBlendOutTime(Animation animComp, string[] mdsNames, string animName, string nextAnimName)
        {
            var anim1 = GetCachedAnimationData(mdsNames, animName, animComp, out var combinedAnimationName1);
            var anim2 = GetCachedAnimationData(mdsNames, nextAnimName, animComp, out var _);


            if (anim1 == null)
            {
                return 0f;
            }

            var anim1Length = MultiTypeCache.AnimationClipCache[combinedAnimationName1].length;
            if (anim2 == null)
            {
                return anim1Length - anim1.BlendOut;
            }

            // TODO - I'm not quite sure if this is correct. Let's look how Open Gothic is handling it.
            return anim1Length - anim1.BlendOut - anim2.BlendIn;
        }

        [CanBeNull]
        public string GetNextAnimationName(Animation animComp, string[] mdsNames, string animName)
        {
            var anim1 = GetCachedAnimationData(mdsNames, animName, animComp, out var _);

            return anim1?.Next;
        }

        [CanBeNull]
        private IAnimation GetCachedAnimationData(string[] mdsNames, string animName, Animation animComp, out string combinedAnimationName)
        {
            combinedAnimationName = null;

            if (animName.IsNullOrEmpty())
            {
                return null;
            }

            foreach (var mdsName in mdsNames.Reverse())
            {
                if (mdsName.IsNullOrEmpty())
                {
                    continue;
                }

                var modelAnimation = ResourceLoader.TryGetModelAnimation(mdsName, animName);
                if (modelAnimation == null)
                {
                    continue;
                }

                combinedAnimationName = GetCombinedAnimationKey(mdsName, animName);

                // For animations: mdhName == mdsName (with different file ending of course ;-))
                var mdhName = mdsName;
                var mds = ResourceLoader.TryGetModelScript(mdsName)!;
                var mdh = ResourceLoader.TryGetModelHierarchy(mdhName);
                var anim = mds.Animations.First(i => i.Name.EqualsIgnoreCase(animName));
                var repeat = anim.Name == anim.Next;
                var go = animComp.gameObject;

                // If we create empty animations with only one frame, Unity will complain. We therefore skip it for now.
                if (anim.FirstFrame == anim.LastFrame)
                {
                    return null;
                }

                if (anim.Direction == AnimationDirection.Backward)
                {
                    Debug.LogWarning(
                        $"Backwards animations not yet handled. Called for >{animName}< from >{mdsName}<. Currently playing Forward.");
                }

                if (!MultiTypeCache.AnimationClipCache.TryGetValue(combinedAnimationName, out var clip))
                {
                    clip = CreateAnimationClip(modelAnimation, mdh, go, repeat, combinedAnimationName);
                    MultiTypeCache.AnimationClipCache[combinedAnimationName] = clip;

                    AddClipEvents(clip, modelAnimation, anim);
                    AddClipEndEvent(anim, clip);
                }

                if (animComp[combinedAnimationName] == null)
                {
                    animComp.AddClip(clip, combinedAnimationName);
                }

                return anim;
            }

            return null;
        }

        private void AddClipEvents(AnimationClip clip, IModelAnimation modelAnimation, IAnimation anim)
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
        /// Adds event at the end of animation.
        /// The event is called on every MonoBehaviour on GameObject where Clip is played.
        /// @see: https://docs.unity3d.com/ScriptReference/AnimationEvent.html
        /// @see: https://docs.unity3d.com/ScriptReference/GameObject.SendMessage.html
        /// </summary>
        private void AddClipEndEvent(IAnimation anim, AnimationClip clip)
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
        private string GetCombinedAnimationKey(string mdsKey, string animKey)
        {
            var preparedMdsKey = GetPreparedKey(mdsKey);
            var preparedAnimKey = GetPreparedKey(animKey);

            return preparedMdsKey + "-" + preparedAnimKey;
        }

        /// <summary>
        /// Basically extract file ending and lower names.
        /// </summary>
        private string GetPreparedKey(string key)
        {
            var lowerKey = key.ToLower();
            var extension = Path.GetExtension(lowerKey);

            if (extension == string.Empty)
            {
                return lowerKey;
            }

            return lowerKey.Replace(extension, "");
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
        private float ClampFrame(int expectedFrame, IModelAnimation modelAnimation, IAnimation anim)
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

        private AnimationClip CreateAnimationClip(IModelAnimation pxAnimation, IModelHierarchy mdh,
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
            foreach (var boneName in boneNames)
            {
                // Skip adding curves for excluded bones
                if (excludeBones != null && excludeBones.Contains(boneName))
                {
                    continue;
                }

                curves.Add(boneName, new List<AnimationCurve>(7));

                // Initialize 7 dimensions. (3x position, 4x rotation)
                curves[boneName].AddRange(Enumerable.Range(0, 7).Select(_ => new AnimationCurve()).ToArray());
            }

            var rootBoneStartCorrection = Vector3.zero;

            // Add KeyFrames from PxSamples
            for (var i = 0; i < pxAnimation.Samples.Count; i++)
            {
                // We want to know what time it is for the animation.
                // Therefore we need to know fps multiplied with current sample. As there are nodeCount samples before a new time starts,
                // we need to add this to the calculation.
                var time = 1 / pxAnimation.Fps * ((float)i / pxAnimation.NodeCount);
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
        private string GetChildPathRecursively(Transform parent, string curName, string currentPath)
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
    }
}
