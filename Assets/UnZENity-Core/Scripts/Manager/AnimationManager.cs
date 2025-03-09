using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Data.Container;
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
        private const string _rootBoneName = "BIP01";


        /// <summary>
        /// Handling animations for baseMds and overlayMds.
        ///
        /// We always apply BlendIn. If there is another animation playing right now, it will be CrossFaded.
        /// If not, it will be started with weight=1 immediately (Unity behavior).
        /// BlendOut will be applied by the callee via Coroutine as it's dynamic based on upcoming animation.
        /// </summary>
        public bool PlayAnimation(Animation animComp, string[] mdsNames, string animName, [CanBeNull] out AnimationContainer animData)
        {
            animData = GetCachedAnimationData(mdsNames, animName, animComp);
            if (animData == null)
            {
                return false;
            }

            // TODO - When calculating BlendOut of previous animation, we say anim1.BlendOut - anim2.BlendIn. Here we only say anim2.BlendIn.
            // TODO - This makes the Fade (e.g.) twice as fast. Okay for now, but could be improved. @see: NpcAnimationHandler.BlendOutCoroutine()
            animComp.CrossFade(animData.FullName, animData.BlendIn);

            return true;
        }

        public float GetBlendOutTime(Animation animComp, string[] mdsNames, string animName, string nextAnimName)
        {
            var anim1 = GetCachedAnimationData(mdsNames, animName, animComp);
            var anim2 = GetCachedAnimationData(mdsNames, nextAnimName, animComp);


            if (anim1 == null)
            {
                return 0f;
            }

            var anim1Length = MultiTypeCache.AnimationDataCache[anim1.FullName].Length;
            if (anim2 == null)
            {
                return anim1Length - anim1.BlendOut;
            }

            // TODO - I'm not quite sure if this is correct. Let's look how Open Gothic is handling it.
            return anim1Length - anim1.BlendOut - anim2.BlendIn;
        }

        /// <summary>
        /// Load animation time from Clip information itself.
        /// </summary>
        public float GetAnimationLength(Animation animComp, string[] mdsNames, string animName)
        {
            foreach (var mdsName in mdsNames.Reverse())
            {
                if (mdsName.IsNullOrEmpty())
                {
                    continue;
                }

                var combinedAnimationName = GetCombinedAnimationKey(mdsName, animName);
                var anim = animComp[combinedAnimationName];

                if (anim != null)
                {
                    return anim.length;
                }
            }

            return 0.0f;
        }

        [CanBeNull]
        public string GetNextAnimationName(Animation animComp, string[] mdsNames, string animName)
        {
            var anim1 = GetCachedAnimationData(mdsNames, animName, animComp);

            return anim1?.Next;
        }

        [CanBeNull]
        private AnimationContainer GetCachedAnimationData(string[] mdsNames, string animName, Animation animComp)
        {
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

                // Try get already cached object.
                var combinedAnimationName = GetCombinedAnimationKey(mdsName, animName);
                if (MultiTypeCache.AnimationDataCache.TryGetValue(combinedAnimationName, out var animData))
                {
                    TryAddAnimationToComponent(animComp, animData);

                    return animData;
                }


                animData = new AnimationContainer()
                {
                    FullName = combinedAnimationName
                };

                var modelAnimation = ResourceLoader.TryGetModelAnimation(mdsName, animName);
                if (modelAnimation == null)
                {
                    continue;
                }

                // For animations: mdhName == mdsName (with different file ending of course ;-))
                var mdhName = mdsName;
                var mds = ResourceLoader.TryGetModelScript(mdsName)!;
                var mdh = ResourceLoader.TryGetModelHierarchy(mdhName);
                animData.Animation = mds.Animations.First(i => i.Name.EqualsIgnoreCase(animName));
                var go = animComp.gameObject;

                // If we create empty animations with only one frame, Unity will complain. We therefore skip it for now.
                if (animData.Animation.FirstFrame == animData.Animation.LastFrame)
                {
                    return null;
                }

                if (animData.Animation.Direction == AnimationDirection.Backward)
                {
                    Debug.LogWarning(
                        $"Backwards animations not yet handled. Called for >{animName}< from >{mdsName}<. Currently playing Forward.");
                }

                animData.Clip = CreateAnimationClip(modelAnimation, mdh, go, animData);
                AddClipEvents(animData.Clip, modelAnimation, animData.Animation);
                SetClipMovementSpeed(animData, modelAnimation, mdh);

                MultiTypeCache.AnimationDataCache[combinedAnimationName] = animData;

                TryAddAnimationToComponent(animComp, animData);

                return animData;
            }

            return null;
        }

        /// <summary>
        /// The created clip needs to be added to the Animation Component before Play() / CrossFade() / BlendIn() will work.
        /// </summary>
        private void TryAddAnimationToComponent(Animation animComp, AnimationContainer animData)
        {
            if (animComp[animData.FullName] == null)
            {
                animComp.AddClip(animData.Clip, animData.FullName);
            }
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
            GameObject rootBone, AnimationContainer animData)
        {
            var clip = new AnimationClip
            {
                legacy = true,
                name = animData.FullName,
                wrapMode = animData.IsLooping ? WrapMode.Loop : WrapMode.Once
            };

            var curves = new Dictionary<string, List<AnimationCurve>>(pxAnimation.NodeCount);
            var boneNames = pxAnimation.NodeIndices.Select(nodeIndex => mdh.Nodes[nodeIndex].Name).ToArray();

            // Initialize array
            foreach (var boneName in boneNames)
            {
                curves.Add(boneName, new List<AnimationCurve>(7));

                // Initialize 7 dimensions. (3x position, 4x rotation)
                curves[boneName].AddRange(Enumerable.Range(0, 7).Select(_ => new AnimationCurve()).ToArray());
            }

            // Add KeyFrames from PxSamples
            for (var i = 0; i < pxAnimation.Samples.Count; i++)
            {
                // We want to know what time it is for the animation.
                // Therefore, we need to know fps multiplied with current sample. As there are nodeCount samples before a new time starts,
                // we need to add this to the calculation.
                var time = 1 / pxAnimation.Fps * ((float)i / pxAnimation.NodeCount);
                var sample = pxAnimation.Samples[i];
                var boneId = i % pxAnimation.NodeCount;
                var boneName = boneNames[boneId];
                var boneList = curves[boneName];
                var uPosition = sample.Position.ToUnityVector();

                // Root bone position will be applied later.
                if (boneName == _rootBoneName)
                {
                    uPosition = default;
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

        /// <summary>
        /// Based on first node (BIP01), we calculate its start position and end position of the animation.
        /// If it's above a threshold, we have a movement animation.
        /// </summary>
        private void SetClipMovementSpeed(AnimationContainer animData, IModelAnimation modelAnim, IModelHierarchy mdh)
        {
            var firstBoneIndex = modelAnim.NodeIndices.First();
            var isRootBoneExisting = mdh.Nodes[firstBoneIndex].Name == _rootBoneName;

            if (!isRootBoneExisting)
            {
                return;
            }

            var boneCount = modelAnim.NodeCount;
            var firstSample = modelAnim.Samples[0];
            var lastSample = modelAnim.Samples[modelAnim.SampleCount - boneCount];

            var movement = (lastSample.Position - firstSample.Position).ToUnityVector();

            // TODO - Create a constant for this 0.4 threshold
            if (movement.sqrMagnitude < 0.4f)
            {
                return;
            }

            animData.IsMoving = true;

            // TODO - We can also check if we do a "movement" calculation based on each frame. Then animations might "woggle" during walk instead of walking on a rubber band.
            animData.MovementSpeed = movement * (modelAnim.FrameCount / modelAnim.Fps);
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
