﻿using System;
using System.Collections.Generic;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Extensions;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Util;
using ZenKit.Vobs;
using Light = ZenKit.Vobs.Light;
using LightType = ZenKit.Vobs.LightType;
using Object = UnityEngine.Object;
using Vector3 = System.Numerics.Vector3;

namespace GUZ.Core.Vob
{
    public class VobManager
    {
        private const string _noSoundName = "nosound.wav";

        private static Dictionary<string, IWorld> _fireVobTreeCache = new();


        /// <summary>
        /// Some VOBs are initialized eagerly (e.g. when there is no performance benefit in doing so later or its needed directly).
        /// </summary>
        public void InitVobImmediately(IVirtualObject vob, GameObject parent)
        {
            // FIXME - Set position and rotation!

            Create(vob, parent);
        }

        /// <summary>
        /// First time a VOB is made visible: Create it.
        /// </summary>
        public void InitVob(GameObject go)
        {
            go.TryGetComponent(out VobLoader loaderComp);

            if (loaderComp == null || loaderComp.IsLoaded)
            {
                return;
            }

            loaderComp.IsLoaded = true;
            var vob = loaderComp.Vob;

            Create(vob, go);
        }

        /// <summary>
        /// To save memory, we can also Destroy Vobs and their Mesh+GO structure.
        /// </summary>
        public void DestroyVob(GameObject go)
        {
            throw new NotImplementedException();
        }
        
        public AudioClip GetSoundClip(string soundName)
        {
            AudioClip clip;

            if (soundName.EqualsIgnoreCase(_noSoundName))
            {
                //instead of decoding nosound.wav which might be decoded incorrectly, just return null
                return null;
            }

            // Bugfix - Normally the data is to get C_SFX_DEF entries from VM. But sometimes there might be the real .wav file stored.
            if (soundName.EndsWithIgnoreCase(".wav"))
            {
                clip = SoundCreator.ToAudioClip(soundName);
            }
            else
            {
                var sfxData = VmInstanceManager.TryGetSfxData(soundName);

                if (sfxData == null)
                {
                    return null;
                }

                if (sfxData.File.EqualsIgnoreCase(_noSoundName))
                {
                    //instead of decoding nosound.wav which might be decoded incorrectly, just return null
                    return null;
                }

                clip = SoundCreator.ToAudioClip(sfxData.File);
            }

            return clip;
        }

        /// <summary>
        /// Generic root method to create a VOB of any type.
        /// Type specific creation will be handled inside further Create*() methods.
        /// </summary>
        private GameObject Create(IVirtualObject vob, GameObject parent)
        {
            switch (vob.Type)
            {
                case VirtualObjectType.zCVob:
                    switch (vob.Visual!.Type)
                    {
                        case VisualType.Decal:
                            if (GameGlobals.Config.Dev.EnableDecalVisuals)
                            {
                                return MeshFactory.CreateVobDecal(vob, (VisualDecal)vob.Visual, parent: parent);
                            }
                            return null;
                        case VisualType.ParticleEffect:
                            if (GameGlobals.Config.Dev.EnableParticleEffects)
                            {
                                return MeshFactory.CreateVobPfx(vob, parent);
                            }
                            return null;
                        default:
                            return CreateDefaultMesh(vob, parent);
                    }
                case VirtualObjectType.oCItem:
                    return CreateItem((Item)vob, parent);
                case VirtualObjectType.oCMOB:
                case VirtualObjectType.oCMobSwitch:
                case VirtualObjectType.zCVobStair:
                case VirtualObjectType.oCMobBed:
                case VirtualObjectType.oCMobWheel:
                case VirtualObjectType.oCMobContainer:
                case VirtualObjectType.oCMobDoor:
                case VirtualObjectType.oCMobLadder:
                    return CreateDefaultMesh(vob, parent);
                case VirtualObjectType.oCMobInter:
                    // FIXME - Re-enable seating. @see #120 - Re-enable Bench/Barrel/Seat/Throne interaction
                    // if (vob.Name.ContainsIgnoreCase("bench") ||
                    //     vob.Name.ContainsIgnoreCase("chair") ||
                    //     vob.Name.ContainsIgnoreCase("throne") ||
                    //     vob.Name.ContainsIgnoreCase("barrelo"))
                    // {
                    //     go = CreateSeat(vob, parent);
                    //     break;
                    // }

                    return CreateDefaultMesh(vob, parent);
                case VirtualObjectType.zCVobAnimate:
                    // TODO - We can outsource the special Animate.Start() logic into Prefab.
                    return CreateAnimatedVob((Animate)vob, parent);
                case VirtualObjectType.oCMobFire:
                    return CreateFire((Fire)vob, parent);
                case VirtualObjectType.zCVobLight:
                    return CreateLight((Light)vob, parent);
                case VirtualObjectType.zCVobSound:
                    if (GameGlobals.Config.Dev.EnableGameSounds)
                    {
                        var go = CreateSound((Sound)vob, parent);
                        GameGlobals.SoundCulling.AddCullingEntry(go);
                        return go;
                    }
                    return null;
                case VirtualObjectType.zCVobSoundDaytime:
                    if (GameGlobals.Config.Dev.EnableGameSounds)
                    {
                        var go = CreateSoundDaytime((SoundDaytime)vob, parent);
                        GameGlobals.SoundCulling.AddCullingEntry(go);
                        return go;
                    }
                    return null;
                default:
                    Debug.LogError($"Unknown VOB type: {vob.Type} - {vob.Name}");
                    return null;
            }
        }

        private GameObject GetPrefab(IVirtualObject vob)
        {
            GameObject go;
            var name = vob.Name;

            switch (vob.Type)
            {
                case VirtualObjectType.oCItem:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobItem, name: name);
                    break;
                case VirtualObjectType.zCVobSpot:
                case VirtualObjectType.zCVobStartpoint:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSpot, name: name);
                    break;
                case VirtualObjectType.zCVobSound:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSound);
                    break;
                case VirtualObjectType.zCVobSoundDaytime:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSoundDaytime, name: name);
                    break;
                case VirtualObjectType.oCZoneMusic:
                case VirtualObjectType.oCZoneMusicDefault:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobMusic, name: name);
                    break;
                case VirtualObjectType.oCMOB:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.Vob, name: name);
                    break;
                case VirtualObjectType.oCMobFire:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobFire, name: name);
                    break;
                case VirtualObjectType.oCMobInter:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobInteractable, name: name);
                    break;
                case VirtualObjectType.oCMobBed:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobBed, name: name);
                    break;
                case VirtualObjectType.oCMobWheel:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobWheel, name: name);
                    break;
                case VirtualObjectType.oCMobSwitch:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSwitch, name: name);
                    break;
                case VirtualObjectType.oCMobDoor:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobDoor, name: name);
                    break;
                case VirtualObjectType.oCMobContainer:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobContainer, name: name);
                    break;
                case VirtualObjectType.oCMobLadder:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobLadder, name: name);
                    break;
                case VirtualObjectType.zCVobAnimate:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobAnimate, name: name);
                    break;
                default:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.Vob, name: name);
                    break;
            }

            // Fill Property data into prefab here
            // Can also be outsourced to a proper method if it becomes a lot.
            go!.GetComponent<VobProperties>().SetData(vob);

            return go;
        }

        private GameObject CreateItem(Item vob, GameObject parent)
        {
            string itemName;

            if (!string.IsNullOrEmpty(vob.Instance))
            {
                itemName = vob.Instance;
            }
            else if (!string.IsNullOrEmpty(vob.Name))
            {
                itemName = vob.Name;
            }
            else
            {
                throw new Exception("Vob Item -> no usable name found.");
            }

            var item = VmInstanceManager.TryGetItemData(itemName);

            if (item == null)
            {
                return null;
            }

            var prefabInstance = GetPrefab(vob);
            var vobObj = CreateItemMesh(item, prefabInstance, parent);

            if (vobObj == null)
            {
                Object.Destroy(prefabInstance); // No mesh created. Delete the prefab instance again.
                Debug.LogError(
                    $"There should be no! object which can't be found n:{vob.Name} i:{vob.Instance}. We need to use >PxVobItem.instance< to do it right!");
                return null;
            }

            vobObj.GetComponent<VobItemProperties>().SetData(vob, item);

            return vobObj;
        }

        private GameObject CreateAnimatedVob(Animate vob, GameObject parent = null)
        {
            var go = CreateDefaultMesh(vob, parent);
            var morph = go.AddComponent<VobAnimateMorph>();
            morph.StartAnimation(vob.Visual!.Name);
            return go;
        }

        private GameObject CreateDefaultMesh(IVirtualObject vob, GameObject parent)
        {
            var go = GetPrefab(vob);
            var meshName = vob.GetVisualName();

            // MDL
            var mdl = ResourceLoader.TryGetModel(meshName);
            if (mdl != null)
            {
                var ret = MeshFactory.CreateVob(meshName, mdl, parent: parent, rootGo: go);

                // A few objects are broken and have no meshes. We need to destroy them immediately again.
                if (ret == null)
                {
                    Object.Destroy(go);
                }

                return ret;
            }

            // MDH+MDM (without MDL as wrapper)
            var mdh = ResourceLoader.TryGetModelHierarchy(meshName);
            var mdm = ResourceLoader.TryGetModelMesh(meshName);
            if (mdh != null && mdm != null)
            {
                var ret = MeshFactory.CreateVob(meshName, mdm, mdh, parent: parent, rootGo: go);

                // A few objects are broken and have no meshes. We need to destroy them immediately again.
                if (ret == null)
                {
                    Object.Destroy(go);
                }

                return ret;
            }

            // MMB
            var mmb = ResourceLoader.TryGetMorphMesh(meshName);
            if (mmb != null)
            {
                var ret = MeshFactory.CreateVob(meshName, mmb, parent: parent, rootGo: go);

                // this is a dynamic object

                // A few objects are broken and have no meshes. We need to destroy them immediately again.
                if (ret == null)
                {
                    Object.Destroy(go);
                }

                return ret;
            }

            // MRM
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(meshName);
            if (mrm != null)
            {
                // If the object is a dynamic one, it will collide.
                var withCollider = vob.CdDynamic;

                var ret = MeshFactory.CreateVob(meshName, mrm, withCollider: withCollider, parent: parent, rootGo: go);

                // A few objects are broken and have no meshes. We need to destroy them immediately again.
                if (ret == null)
                {
                    Object.Destroy(go);
                }

                return ret;
            }

            Debug.LogWarning($">{meshName}<'s has no mdl/mrm.");
            return null;
        }

        private GameObject CreateItemMesh(ItemInstance item, GameObject go, GameObject parent)
        {
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);

            if (mrm != null)
            {
                return MeshFactory.CreateVob(item.Visual, mrm, parent: parent, rootGo: go, useTextureArray: false);
            }

            // shortbow (itrw_bow_l_01) has no mrm, but has mmb
            var mmb = ResourceLoader.TryGetMorphMesh(item.Visual);

            return MeshFactory.CreateVob(item.Visual, mmb, parent: parent, rootGo: go);
        }

        /// <summary>
        /// Some fire slots have the light too low to cast light onto the mesh and the surroundings.
        /// </summary>
        [CanBeNull]
        private GameObject CreateFire(Fire vob, GameObject parent = null)
        {
            var go = CreateDefaultMesh(vob, parent);

            if (vob.VobTree == "")
            {
                return null;
            }

            if (!_fireVobTreeCache.TryGetValue(vob.VobTree.ToLower(), out var vobTree))
            {
                vobTree = ResourceLoader.TryGetWorld(vob.VobTree, GameContext.GameVersionAdapter.Version);
                _fireVobTreeCache.Add(vob.VobTree.ToLower(), vobTree);
            }

            foreach (var vobRoot in vobTree!.RootObjects)
            {
                ResetVobTreePositions(vobRoot.Children, vobRoot.Position, vobRoot.Rotation);
                vobRoot.Position = Vector3.Zero;
            }

            CreateFireVobs(vobTree.RootObjects, go.FindChildRecursively(vob.Slot) ?? go);

            return go;
        }

        /// <summary>
        /// A fire itself is a .zen file which contains children.
        /// </summary>
        private void CreateFireVobs(List<IVirtualObject> vobs, GameObject parent)
        {
            foreach (var vob in vobs)
            {
                // Call normal mesh and Prefab loading logic
                var go = Create(vob, parent);

                // Iterate over all children
                CreateFireVobs(vob.Children, go);
            }
        }

        /// <summary>
        /// Reset the positions of the objects in the list to subtract position of the parent
        /// In the zen files all the vobs have the position represented for the world not per parent
        /// and as we might load multiple copies of the same vob tree but for different parents we need to reset the position
        /// </summary>
        private static void ResetVobTreePositions(List<IVirtualObject> vobList,
            Vector3 position, Matrix3x3 rotation)
        {
            if (vobList == null)
            {
                return;
            }

            foreach (var vob in vobList)
            {
                ResetVobTreePositions(vob.Children, position, rotation);

                vob.Position -= position;

                vob.Rotation = new Matrix3x3(vob.Rotation.M11 - rotation!.M11, vob.Rotation.M12 - rotation.M12,
                    vob.Rotation.M13 - rotation.M13, vob.Rotation.M21 - rotation.M21,
                    vob.Rotation.M22 - rotation.M22, vob.Rotation.M23 - rotation.M23,
                    vob.Rotation.M31 - rotation.M31, vob.Rotation.M32 - rotation.M32,
                    vob.Rotation.M33 - rotation.M33);
            }
        }

        private GameObject CreateLight(Light vob, GameObject parent)
        {
            if (vob.LightStatic)
            {
                Debug.LogWarning($"Non-static lights aren't handled so far. go={vob.Name}");
                return null;
            }

            var vobObj = GetPrefab(vob);
            vobObj.name = $"{vob.LightType} Light {vob.Name}";
            vobObj.SetParent(parent);

            var lightComp = vobObj.AddComponent<StationaryLight>();
            lightComp.Color = new Color(vob.Color.R / 255f, vob.Color.G / 255f, vob.Color.B / 255f, vob.Color.A / 255f);
            lightComp.Type = vob.LightType == LightType.Point
                ? UnityEngine.LightType.Point
                : UnityEngine.LightType.Spot;
            lightComp.Range = vob.Range * .01f;
            lightComp.SpotAngle = vob.ConeAngle;
            lightComp.Intensity = 1;

            return vobObj;
        }

        [CanBeNull]
        private GameObject CreateSound(Sound vob, GameObject parent)
        {
            var go = GetPrefab(vob);
            go.name = $"{vob.SoundName}";
            go.SetParent(parent);

            // This value is always true when a new game/world is loaded. (Compared with G1 save game.)
            vob.ShowVisual = false;
            vob.IsAllowedToRun = true;

            // We don't want to have sound when we boot the game async for 30 seconds in non-spatial blend mode.
            go.SetActive(false);

            var source = go.GetComponent<AudioSource>();

            PrepareAudioSource(source, vob);
            source.clip = GetSoundClip(vob.SoundName);

            go.GetComponent<VobSoundProperties>().SoundData = vob;
            go.GetComponent<SoundHandler>().PrepareSoundHandling();

            return go;
        }

        /// <summary>
        /// Creating AudioSource from PxVobSoundDaytimeData is very similar to PxVobSoundData one.
        /// There are only two differences:
        ///     1. This one has two AudioSources
        ///     2. The sources will be toggled during gameplay when start/end time is reached.
        /// </summary>
        [CanBeNull]
        private GameObject CreateSoundDaytime(SoundDaytime vob, GameObject parent)
        {
            var go = GetPrefab(vob);
            go.name = $"{vob.SoundName}-{vob.SoundNameDaytime}";
            go.SetParent(parent);

            // We don't want to have sound when we boot the game async for 30 seconds in non-spatial blend mode.
            go.SetActive(false);
            go.SetParent(parent);

            var sources = go.GetComponents<AudioSource>();

            PrepareAudioSource(sources[0], vob);
            sources[0].clip = GetSoundClip(vob.SoundName);

            PrepareAudioSource(sources[1], vob);
            sources[1].clip = GetSoundClip(vob.SoundNameDaytime);

            go.GetComponent<VobSoundDaytimeProperties>().SoundDaytimeData = vob;
            go.GetComponent<SoundDaytimeHandler>().PrepareSoundHandling();

            return go;
        }

        private void PrepareAudioSource(AudioSource source, Sound soundData)
        {
            source.maxDistance = soundData.Radius / 100f; // Gothic's values are in cm, Unity's in m.
            source.volume = soundData.Volume / 100f; // Gothic's volume is 0...100, Unity's is 0...1.

            // Random sounds shouldn't play initially, but after certain time.
            source.playOnAwake = soundData.InitiallyPlaying && soundData.Mode != SoundMode.Random;
            source.loop = soundData.Mode == SoundMode.Loop;
            source.spatialBlend = soundData.Ambient3d ? 1f : 0f;
        }
    }
}
