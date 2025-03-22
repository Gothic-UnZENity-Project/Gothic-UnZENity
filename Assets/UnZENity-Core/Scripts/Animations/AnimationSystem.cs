using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Npc;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit;
using EventType = ZenKit.EventType;
using Object = UnityEngine.Object;

namespace GUZ.Core.Animations
{
    /// <summary>
    /// NPC component to handle animations. The Blending is using the official Gothic animation information:
    /// https://www.worldofgothic.de/modifikation/index.php?go=animationen
    /// </summary>
    public class AnimationSystem : BasePlayerBehaviour
    {
        // These properties are normally private. For the Debug Window in Editor Mode, we allow to read them.
#if UNITY_EDITOR
        public List<AnimationTrackInstance> DebugTrackInstances => _trackInstances;
        public string[] DebugBoneNames => _boneNames;
#endif

        public Transform RootBone;

        // Caching bone Transforms makes it faster to apply them to animations later.
        private string[] _boneNames;
        private Transform[] _bones;
        private Vector3[] _meshBonePos;
        private Quaternion[] _meshBoneRot;
        private List<AnimationTrackInstance> _trackInstances = new();

        protected override void Awake()
        {
            base.Awake();

            // Cached object which will be used later.
            NpcData.PrefabProps.AnimationSystem = this;
        }

        private void Start()
        {
            Dictionary<string, Transform> bones = new();
            CollectBones(RootBone, bones);

            _boneNames = bones.Keys.ToArray();
            _bones = bones.Values.ToArray();
            _meshBonePos = _bones.Select(i => i.transform.localPosition).ToArray();
            _meshBoneRot = _bones.Select(i => i.transform.localRotation).ToArray();
        }

        private void CollectBones(Transform bone, Dictionary<string, Transform> bones)
        {
            // Bones always start with BIP01. Other elements are Prefab specific.
            if (bone.name.StartsWith("BIP01") || bone.name.StartsWith("ZS_"))
            {
                bones.Add(bone.name, bone);
            }

            foreach (Transform child in bone)
            {
                CollectBones(child, bones);
            }
        }

        public bool PlayAnimation(string animationName)
        {
            var newTrack = AnimationManager2.GetTrack(animationName, Properties.MdsNameBase, Properties.MdsNameOverlay);

            if (newTrack == null)
            {
                return false;
            }

            // FIXME - Now we need to handle animation flags: M - Move and R - Rotate.
            //         Then S_ROTATEL will work properly and stop once rotated enough.
            Debug.Log($"Playing animation: {animationName}");

            if (IsAlreadyPlaying(newTrack))
            {
                return true;
            }

            var newTrackInstance = new AnimationTrackInstance(newTrack);
            var newTrackLayer = newTrackInstance.Track.Layer;

            // Handle existing Track Blending based on layer of new Track.
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var trackInstance = _trackInstances[i];
                var trackLayer = trackInstance.Track.Layer;

                if (trackLayer < newTrackLayer)
                {
                    BlendOutTrackBones(trackInstance, newTrackInstance);
                }
                else if (trackLayer == newTrackLayer)
                {
                    BlendOutTrack(trackInstance, newTrackInstance);
                }
                else if (trackLayer > newTrackLayer)
                {
                    StopTrackBones(trackInstance, newTrackInstance);
                }
            }

            // If this is the first animation on NPC, we need no BlendIn, simply start all bones at frame 0.
            // TODO - Could be handled differently as a Layer0 animation aka Bone pose of ModelMesh itself.
            if (_trackInstances.IsEmpty())
            {
                newTrackInstance.SetPlayState();
            }

            _trackInstances.Add(newTrackInstance);

            // Sort descending order (e.g. Layer20, L2, L2, L1)
            // It's needed, as in the end, we will apply some blend weight fixes on the lowest level animation.
            _trackInstances.Sort((a, b) => b.Track.Layer.CompareTo(a.Track.Layer));

            return true;
        }

        private bool IsAlreadyPlaying(AnimationTrack newTrack)
        {
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                if (newTrack.Animation.Name == _trackInstances[i].Track.Animation.Name)
                {
                    return true;
                }
            }

            return false;
        }

        public bool PlayIdleAnimation()
        {
            return PlayAnimation(GetIdleAnimationName());
        }

        public float GetAnimationDuration(string animationName)
        {
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var instance = _trackInstances[i];
                if (instance.Track.Animation.Name.EqualsIgnoreCase(animationName))
                {
                    return instance.Track.Duration;
                }
            }

            return 0f;
        }

        public void StopAnimation(string stoppingAnimationName)
        {
            AnimationTrackInstance instanceToStop = null;

            // Fetch and blend out Animation.
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var instance = _trackInstances[i];
                // If animation is found, then mark it as "BlendOut"
                if (instance.Track.Animation.Name.EqualsIgnoreCase(stoppingAnimationName))
                {
                    instanceToStop = instance;
                    instance.BlendOutTrack(instance.Track.Animation.BlendOut);
                    // Do not break. We could potentially need to stop multiple instances of the same animation.
                }
            }

            if (instanceToStop == null)
            {
                return;
            }

            // Ramp up bones on animation with lower level as the higher level bones will blend out.
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var instance = _trackInstances[i];
                if (instance.Track.Animation.Name.EqualsIgnoreCase(stoppingAnimationName))
                {
                    continue;
                }

                if (instance.Track.Layer < instanceToStop.Track.Layer)
                {
                    instance.BlendInBones(instanceToStop.Track.BoneNames, instanceToStop.Track.Animation.BlendOut);
                }
            }
        }

        /// <summary>
        /// Higher level Animations might have only a few bones which might be handled by a lower layer animation.
        /// We therefore need to blend out the other animation(s) Bones, not the whole animation.
        /// </summary>
        private void BlendOutTrackBones(AnimationTrackInstance lowerLayerTrack, AnimationTrackInstance higherLayerTrack)
        {
            lowerLayerTrack.BlendOutBones(higherLayerTrack.Track.BoneNames, higherLayerTrack.Track.Animation.BlendIn);
        }

        /// <summary>
        /// Tracks on the same layer will either need to stop immediately or blend out at the current frame.
        /// </summary>
        private void BlendOutTrack(AnimationTrackInstance oldTrack, AnimationTrackInstance newTrack)
        {
            // From Documentation:
            // E: Diese Flag sorgt dafür, dass die Ani erst gestartet wird, wenn eine zur Zeit aktive Ani im selben Layer ihren letzten Frame
            // erreicht hat und somit beendet wird. Sinnvoll z.B. in folgenden Fall: ani "s_walk", ani "t_walk_2_stand", ani "s_stand", wobei alle Anis als ASC-Anis vorliegen.
            var isStartAtLastFrame = newTrack.Track.Animation.Flags.HasFlag(AnimationFlags.Queue);

            if (isStartAtLastFrame)
            {
                // FIXME - Implement
                Debug.LogError("AnimationFlags.Queue not implemented yet.");
            }
            // else
            // {
                oldTrack.BlendOutTrack(newTrack.Track.Animation.BlendIn);
            // }
        }

        /// <summary>
        /// The current instance starts blending out, which means, that lower layer bones can blend in again.
        /// </summary>
        private void BlendInOtherTrackBones(AnimationTrackInstance instanceBlendingOut)
        {
            foreach (var trackInstance in _trackInstances)
            {
                if (trackInstance.Track.Layer < instanceBlendingOut.Track.Layer)
                {
                    trackInstance.BlendInBones(instanceBlendingOut.Track.BoneNames, instanceBlendingOut.Track.Animation.BlendOut);
                }
            }
        }

        /// <summary>
        /// If we start a new instance, we need to apply, which bones should not be started, as e.g. T_DIALOGGESTURE_ from
        /// a higher level forced the animation to stop bones.
        /// </summary>
        private void StopTrackBones(AnimationTrackInstance lowerLayerTrack, AnimationTrackInstance higherLayerTrack)
        {
            var bonesToSkip = new List<string>();
            for (var i = 0; i < higherLayerTrack.Track.BoneCount; i++)
            {
                // TODO - If a higher layer bone is blending out, we should align lower level bone blend in times so that we have 1f weight at all time.
                // If the animation has bones in a BlendOut state, we do not skip them for our lower level animation.
                if (higherLayerTrack.BoneStates[i] == AnimationState.BlendOut)
                {
                    continue;
                }

                bonesToSkip.Add(higherLayerTrack.Track.BoneNames[i]);
            }

            lowerLayerTrack.BlendOutBones(bonesToSkip.ToArray(), 0f);
        }


        private void Update()
        {
            if (_trackInstances.Count == 0)
            {
                return;
            }

            // Update all tracks
            // ToArray() -> We need to copy the array, as we are modifying it.
            foreach (var instance in _trackInstances.ToArray())
            {
                switch (instance.Update(Time.deltaTime))
                {
                    case AnimationState.None:
                        break;
                    case AnimationState.BlendIn:
                        break;
                    case AnimationState.Play:
                        break;
                    case AnimationState.BlendOut:
                        PlayAnimation(instance.Track.Animation.Next);
                        BlendInOtherTrackBones(instance);
                        break;
                    case AnimationState.Stop:
                        _trackInstances.Remove(instance);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            ApplyFinalPose();
            ApplyFinalMovement();
            ApplyEvents();
        }

        private void ApplyFinalPose()
        {
            // Accumulate poses from all tracks
            for (var boneIndex = 0; boneIndex < _boneNames.Length; boneIndex++)
            {
                var boneName = _boneNames[boneIndex];
                var bone = _bones[boneIndex];

                var finalPosition = Vector3.zero;
                var finalRotation = Quaternion.identity;

                var boneWeightSum = 0f;
                for (var i = 0; i < _trackInstances.Count; i++)
                {
                    var trackInstance = _trackInstances[i];
                    var trackInstanceBoneIndex = trackInstance.GetBoneIndex(boneName);

                    if (trackInstanceBoneIndex == -1)
                    {
                        continue;
                    }

                    var trackInstanceBoneWeight = trackInstance.BoneBlendWeights[trackInstanceBoneIndex];
                    boneWeightSum += trackInstanceBoneWeight;

                    // If we have some fast-changing situations like T_DIALOGGESTURE_ is blending out and another one is blending in - in between,
                    // We need to dynamically handle overweighting. We do so by reducing weights on lower layers
                    // (e.g. in the DIALOG case, T_WALK would be blended out more, as we sort the trackInstance list from high to low layer).
                    if (boneWeightSum > 1f)
                    {
                        // Fetch amount of weight higher than 1f on boneWeightSum
                        var amountOfOverWeight = boneWeightSum - 1f;
                        boneWeightSum = 1f;

                        trackInstanceBoneWeight -= amountOfOverWeight;
                    }

                    trackInstance.GetBonePose(trackInstanceBoneIndex, out var position, out var rotation, trackInstanceBoneWeight);
                    finalPosition += position;
                    finalRotation *= rotation;
                }

                // If we under blended the current object, we need to apply positions from the mesh itself.
                // Otherwise, we might have some 0.1f weight of animations alone and the NPC will implode like a black hole at 0,0,0.
                // This should be a rare case where we won't have a sum of 1.0. Just a safety treatment.
                if (boneWeightSum < 1f)
                {
                    finalPosition += _meshBonePos[boneIndex] * (1 - boneWeightSum);
                    finalRotation *= Quaternion.Slerp(Quaternion.identity, _meshBoneRot[boneIndex], 1 - boneWeightSum);
                }

                bone.localPosition = finalPosition;
                bone.localRotation = finalRotation;
            }
        }

        private void ApplyFinalMovement()
        {
            var finalMovement = Vector3.zero;
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var trackInstance = _trackInstances[i];

                if (!trackInstance.Track.IsMoving)
                {
                    continue;
                }

                var boneIndex = trackInstance.GetBoneIndex(Constants.Animations.RootBoneName);
                if (boneIndex == -1)
                {
                    continue;
                }

                finalMovement += trackInstance.Track.MovementSpeed * trackInstance.BoneBlendWeights[boneIndex] * Time.deltaTime;
            }

            // Pos change is applied with rotated value.
            PrefabProps.Go.transform.localPosition += PrefabProps.Go.transform.rotation * finalMovement;
        }

        private void ApplyEvents()
        {
            for (var i = 0; i < _trackInstances.Count; i++)
            {
                var trackInstance = _trackInstances[i];

                ApplyEventTags(trackInstance);
                ApplySfxEvents(trackInstance);
                ApplyPfxEvents(trackInstance);
                ApplyMorphEvents(trackInstance);
            }
        }

        private void ApplyEventTags(AnimationTrackInstance trackInstance)
        {
            var eventTags = trackInstance.GetPendingEventTags();
            if (eventTags == null)
            {
                return;
            }

            foreach (var eventTag in eventTags)
            {
                switch (eventTag.Type)
                {
                    case EventType.ItemInsert:
                        InsertItem(eventTag.Slots.Item1, eventTag.Slots.Item2);
                        break;
                    case EventType.ItemDestroy:
                    case EventType.ItemRemove:
                        RemoveItem();
                        break;
                    case EventType.TorchInventory:
                        // TODO - I assume this means: if torch is in inventory, then put it out. But not really sure. Need a NPC with real usage of it to predict right.
                        break;
                    default:
                        Debug.LogWarning($"EventType.type {eventTag.Type} not yet supported.");
                        break;
                }
            }
        }

        private void ApplySfxEvents(AnimationTrackInstance trackInstance)
        {
            var sfxEvents = trackInstance.GetPendingSoundEffects();
            if (sfxEvents == null)
            {
                return;
            }

            foreach (var sfx in sfxEvents)
            {
                var clip = GameGlobals.Vobs.GetSoundClip(sfx.Name);
                PrefabProps.NpcSound.clip = clip;
                PrefabProps.NpcSound.maxDistance = sfx.Range.ToMeter();
                PrefabProps.NpcSound.Play();
            }
        }

        private void ApplyPfxEvents(AnimationTrackInstance trackInstance)
        {
            var pfxEvents = trackInstance.GetPendingParticleEffects();
            if (pfxEvents == null)
            {
                return;
            }

            foreach (var pfx in pfxEvents)
            {
                Debug.LogWarning($"Particle Effects are not yet supported. {pfx.Name}");
            }
        }

        private void ApplyMorphEvents(AnimationTrackInstance trackInstance)
        {
            var morphEvents = trackInstance.GetPendingMorphAnimations();
            if (morphEvents == null)
            {
                return;
            }

            foreach (var morph in morphEvents)
            {
                var type = PrefabProps.HeadMorph.GetAnimationTypeByName(morph.Animation);

                PrefabProps.HeadMorph.StartAnimation(Properties.BodyData.Head, type);
            }
        }

        private string GetIdleAnimationName()
        {
            string walkMode;
            switch (Properties.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    walkMode = "WALK";
                    break;
                case VmGothicEnums.WalkMode.Run:
                    walkMode = "RUN";
                    break;
                case VmGothicEnums.WalkMode.Sneak:
                    walkMode = "SNEAK";
                    break;
                case VmGothicEnums.WalkMode.Water:
                    walkMode = "WATER";
                    break;
                case VmGothicEnums.WalkMode.Swim:
                    walkMode = "SWIM";
                    break;
                case VmGothicEnums.WalkMode.Dive:
                    walkMode = "DIVE";
                    break;
                default:
                    Debug.LogWarning($"Animation of type {Properties.WalkMode} not yet implemented.");
                    return "";
            }

            return $"S_{walkMode}";
        }

        private void InsertItem(string slot1, string slot2)
        {
            if (slot2.Any())
            {
                throw new Exception("Slot 2 is set but not yet handled by InsertItem as AnimationEvent.");
            }

            var slotGo = PrefabProps.Bip01.gameObject.FindChildRecursively(slot1);

            GameGlobals.Vobs.CreateItemMesh(Properties.CurrentItem, slotGo);

            Properties.UsedItemSlot = slot1;
        }

        private void RemoveItem()
        {
            // Some animations need to force remove items, some not.
            if (Properties.UsedItemSlot == "")
            {
                return;
            }

            var slotGo = PrefabProps.Bip01.FindChildRecursively(Properties.UsedItemSlot);
            var item = slotGo!.GetChild(0);

            Object.Destroy(item.gameObject);
        }
    }
}
