using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Context;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Demo;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Player.Interactive;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using GUZ.Core.Vob;
using GUZ.Core.Vob.WayNet;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Util;
using ZenKit.Vobs;
using Debug = UnityEngine.Debug;
using Light = ZenKit.Vobs.Light;
using LightType = ZenKit.Vobs.LightType;
using Material = UnityEngine.Material;
using Object = UnityEngine.Object;
using Vector3 = System.Numerics.Vector3;

namespace GUZ.Core.Creator
{
    public static class VobCreator
    {
        private static Dictionary<VirtualObjectType, GameObject> _parentGosTeleport = new();
        private static Dictionary<VirtualObjectType, GameObject> _parentGosNonTeleport = new();

        private static VirtualObjectType[] _nonTeleportTypes =
        {
            VirtualObjectType.oCItem,
            VirtualObjectType.oCMobLadder,
            VirtualObjectType.oCZoneMusic,
            VirtualObjectType.oCZoneMusicDefault,
            VirtualObjectType.zCVobSound,
            VirtualObjectType.zCVobSoundDaytime,
            VirtualObjectType.zCVobAnimate
        };

        private static GameObject _vobsGo;
        private static GameObject _teleportGo;
        private static GameObject _nonTeleportGo;

        private static int _totalVObs;
        private static int _vobsPerFrame;
        private static int _createdCount;
        private static List<GameObject> _cullingVobObjects = new();
        private static Dictionary<string, IWorld> _vobTreeCache = new();

        static VobCreator()
        {
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(PostWorldLoaded);
        }

        private static void PostWorldLoaded(GameObject playerGo)
        {
            /*
             * We need to set the Teleportation area after adding mesh to VOBs.
             */
            var interactionManager = GameGlobals.Scene.InteractionManager.GetComponent<XRInteractionManager>();
            var teleportationArea = _teleportGo.AddComponent<TeleportationArea>();
            if (interactionManager != null)
            {
                teleportationArea.interactionManager = interactionManager;
            }
        }

        public static async Task CreateAsync(GameConfiguration config, LoadingManager loading, List<IVirtualObject> vobs, int vobsPerFrame)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            PreCreateVobs(vobs, vobsPerFrame);
            await CreateVobs(config, loading, vobs);
            PostCreateVobs();
            stopwatch.Stop();
            Debug.Log($"Created vobs in {stopwatch.Elapsed.TotalSeconds} s");
        }

        private static void PreCreateVobs(List<IVirtualObject> vobs, int vobsPerFrame)
        {
            _totalVObs = GetTotalVobCount(vobs);

            _createdCount = 0;
            _cullingVobObjects.Clear();
            _vobsPerFrame = vobsPerFrame;

            _vobsGo = new GameObject("VOBs");
            _teleportGo = new GameObject("Teleport");
            _nonTeleportGo = new GameObject("NonTeleport");
            _teleportGo.SetParent(_vobsGo);
            _nonTeleportGo.SetParent(_vobsGo);

            _parentGosTeleport = new Dictionary<VirtualObjectType, GameObject>();
            _parentGosNonTeleport = new Dictionary<VirtualObjectType, GameObject>();

            CreateParentVobObjectTeleport(_teleportGo);
            CreateParentVobObjectNonTeleport(_nonTeleportGo);
        }

        private static int GetTotalVobCount(List<IVirtualObject> vobs)
        {
            return vobs.Count + vobs.Sum(vob => GetTotalVobCount(vob.Children));
        }

        private static async Task CreateVobs(GameConfiguration config, LoadingManager loading,
            List<IVirtualObject> vobs, GameObject parent = null, bool reparent = false)
        {
            foreach (var vob in vobs)
            {
                GameObject go = null;

                // Debug - Skip loading if not wanted.
                if (config.SpawnVOBTypes.Value.IsEmpty() || config.SpawnVOBTypes.Value.Contains(vob.Type))
                {
                    go = reparent ? LoadVob(config, vob, parent) : LoadVob(config, vob);
                }

                AddToMobInteractableList(vob, go);

                if (++_createdCount % _vobsPerFrame == 0)
                {
                    await Task.Yield(); // Wait for the next frame
                }

                loading?.AddProgress(LoadingManager.LoadingProgressType.VOb, 1f / _totalVObs);

                // Recursive creating sub-vobs
                await CreateVobs(config, loading, vob.Children, go, reparent);
            }
        }

        [CanBeNull]
        private static GameObject LoadVob(GameConfiguration config, IVirtualObject vob, GameObject parent = null)
        {
            GameObject go = null;
            switch (vob.Type)
            {
                case VirtualObjectType.oCItem:
                {
                    go = CreateItem((Item)vob, parent);
                    _cullingVobObjects.Add(go);
                    break;
                }
                case VirtualObjectType.zCVobLight:
                {
                    go = CreateLight((Light)vob, parent);
                    break;
                }
                case VirtualObjectType.oCMobContainer:
                {
                    go = CreateMobContainer((Container)vob, parent);
                    _cullingVobObjects.Add(go);
                    break;
                }
                case VirtualObjectType.zCVobSound:
                {
                    if (config.EnableGameSounds)
                    {
                        go = CreateSound((Sound)vob, parent);
                        GameGlobals.SoundCulling.AddCullingEntry(go);
                    }

                    break;
                }
                case VirtualObjectType.zCVobSoundDaytime:
                {
                    if (config.EnableGameSounds)
                    {
                        go = CreateSoundDaytime((SoundDaytime)vob, parent);
                        GameGlobals.SoundCulling.AddCullingEntry(go);
                    }

                    break;
                }
                case VirtualObjectType.oCZoneMusic:
                case VirtualObjectType.oCZoneMusicDefault:
                {
                    if (config.EnableGameMusic)
                    {
                        go = CreateZoneMusic((ZoneMusic)vob, parent);
                    }

                    break;
                }
                case VirtualObjectType.zCVobSpot:
                case VirtualObjectType.zCVobStartpoint:
                {
                    go = CreateSpot(vob, parent, config.ShowFreePoints);
                    break;
                }
                case VirtualObjectType.oCMobLadder:
                {
                    go = CreateLadder(vob, parent);
                    _cullingVobObjects.Add(go);
                    break;
                }
                case VirtualObjectType.oCTriggerChangeLevel:
                {
                    go = CreateTriggerChangeLevel((TriggerChangeLevel)vob, parent);
                    break;
                }
                case VirtualObjectType.zCVob:
                {
                    if (vob.Visual == null)
                    {
                        CreateDebugObject(vob, parent);
                        break;
                    }

                    switch (vob.Visual!.Type)
                    {
                        case VisualType.Decal:
                            if (config.EnableDecalVisuals)
                            {
                                go = CreateDecal(vob, parent);
                            }

                            break;
                        case VisualType.ParticleEffect:
                            if (config.EnableParticleEffects)
                            {
                                go = CreatePfx(vob, parent);
                            }

                            break;
                        default:
                            go = CreateDefaultMesh(vob, parent);
                            break;
                    }

                    _cullingVobObjects.Add(go);
                    break;
                }
                case VirtualObjectType.oCMobFire:
                {
                    go = CreateFire(config, (Fire)vob, parent);
                    _cullingVobObjects.Add(go);
                    break;
                }
                case VirtualObjectType.oCMobInter:
                {
                    if (vob.Name.ContainsIgnoreCase("bench") ||
                        vob.Name.ContainsIgnoreCase("chair") ||
                        vob.Name.ContainsIgnoreCase("throne") ||
                        vob.Name.ContainsIgnoreCase("barrelo"))
                    {
                        go = CreateSeat(vob, parent);
                        _cullingVobObjects.Add(go);
                        break;
                    }

                    go = CreateDefaultMesh(vob);
                    _cullingVobObjects.Add(go);
                    break;
                }
                case VirtualObjectType.oCMobDoor:
                case VirtualObjectType.oCMobSwitch:
                case VirtualObjectType.oCMOB:
                case VirtualObjectType.zCVobStair:
                case VirtualObjectType.oCMobBed:
                case VirtualObjectType.oCMobWheel:
                {
                    go = CreateDefaultMesh(vob, parent);
                    _cullingVobObjects.Add(go);
                    break;
                }
                case VirtualObjectType.zCVobAnimate:
                {
                    go = CreateAnimatedVob((Animate)vob, parent);
                    _cullingVobObjects.Add(go);
                    break;
                }
                case VirtualObjectType.oCNpc:
                {
                    if (vob.Name.EqualsIgnoreCase(Constants.DaedalusHeroInstanceName))
                    {
                        GameGlobals.Scene.SetStart(vob.Position.ToUnityVector(), vob.Rotation.ToUnityQuaternion());
                        break;
                    }

                    if (!config.EnableNpcs)
                    {
                        break;
                    }

                    go = CreateNpc((ZenKit.Vobs.Npc)vob);
                    break;
                }
                case VirtualObjectType.zCVobScreenFX:
                case VirtualObjectType.zCTriggerWorldStart:
                case VirtualObjectType.zCTriggerList:
                case VirtualObjectType.oCCSTrigger:
                case VirtualObjectType.oCTriggerScript:
                case VirtualObjectType.zCVobLensFlare:
                case VirtualObjectType.zCMoverController:
                case VirtualObjectType.zCPFXController:
                case VirtualObjectType.zCMover:
                case VirtualObjectType.zCVobLevelCompo:
                case VirtualObjectType.zCZoneZFog:
                case VirtualObjectType.zCZoneZFogDefault:
                case VirtualObjectType.zCZoneVobFarPlane:
                case VirtualObjectType.zCZoneVobFarPlaneDefault:
                {
                    // FIXME - not yet implemented.
                    break;
                }
                default:
                {
                    throw new Exception(
                        $"VobType={vob.Type} not yet handled. And we didn't know we need to do so. ;-)");
                }
            }

            return go;
        }

        /// <summary>
        /// Some fire slots have the light too low to cast light onto the mesh and the surroundings.
        /// </summary>
        private static GameObject CreateFire(GameConfiguration config, Fire vob, GameObject parent = null)
        {
            var go = CreateDefaultMesh(vob, parent);

            if (vob.VobTree == "")
            {
                return go;
            }

            if (!_vobTreeCache.TryGetValue(vob.VobTree.ToLower(), out var vobTree))
            {
                vobTree = ResourceLoader.TryGetWorld(vob.VobTree, GameVersion.Gothic1);
                _vobTreeCache.Add(vob.VobTree.ToLower(), vobTree);
            }

            foreach (var vobRoot in vobTree.RootObjects)
            {
                ResetVobTreePositions(vobRoot.Children, vobRoot.Position, vobRoot.Rotation);
                vobRoot.Position = Vector3.Zero;
            }

            CreateVobs(config, null, vobTree.RootObjects, go.FindChildRecursively(vob.Slot) ?? go, true);

            return go;
        }

        /// <summary>
        /// Reset the positions of the objects in the list to subtract position of the parent
        /// In the zen files all the vobs have the position represented for the world not per parent
        /// and as we might load multiple copies of the same vob tree but for different parents we need to reset the position
        /// </summary>
        /// <param name="vobList"></param>
        /// <param name="position"></param>
        private static void ResetVobTreePositions(List<IVirtualObject> vobList, Vector3 position = default,
            Matrix3x3 rotation = default)
        {
            if (vobList == null)
            {
                return;
            }

            foreach (var vob in vobList)
            {
                ResetVobTreePositions(vob.Children, position, rotation);

                vob.Position -= position;

                vob.Rotation = new Matrix3x3(vob.Rotation.M11 - rotation.M11, vob.Rotation.M12 - rotation.M12,
                    vob.Rotation.M13 - rotation.M13, vob.Rotation.M21 - rotation.M21,
                    vob.Rotation.M22 - rotation.M22, vob.Rotation.M23 - rotation.M23,
                    vob.Rotation.M31 - rotation.M31, vob.Rotation.M32 - rotation.M32,
                    vob.Rotation.M33 - rotation.M33);
            }
        }

        private static void PostCreateVobs()
        {
            GameGlobals.VobMeshCulling.PrepareVobCulling(_cullingVobObjects);

            _vobTreeCache.ClearAndReleaseMemory();

            // TODO - warnings about "not implemented" - print them once only.
            foreach (var var in new[]
                     {
                         VirtualObjectType.zCVobScreenFX,
                         VirtualObjectType.zCVobAnimate,
                         VirtualObjectType.zCTriggerWorldStart,
                         VirtualObjectType.zCTriggerList,
                         VirtualObjectType.oCCSTrigger,
                         VirtualObjectType.oCTriggerScript,
                         VirtualObjectType.zCVobLensFlare,
                         VirtualObjectType.zCMoverController,
                         VirtualObjectType.zCPFXController
                     })
            {
                Debug.LogWarning($"{var} not yet implemented.");
            }
        }

        private static GameObject GetPrefab(IVirtualObject vob)
        {
            GameObject go;
            var name = vob.Name;

            switch (vob.Type)
            {
                case VirtualObjectType.oCItem:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobItem);
                    break;
                case VirtualObjectType.zCVobSpot:
                case VirtualObjectType.zCVobStartpoint:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSpot);
                    break;
                case VirtualObjectType.zCVobSound:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSound);
                    break;
                case VirtualObjectType.zCVobSoundDaytime:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSoundDaytime);
                    break;
                case VirtualObjectType.oCZoneMusic:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobMusic);
                    break;
                case VirtualObjectType.oCMOB:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.Vob);
                    break;
                case VirtualObjectType.oCMobFire:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobFire);
                    break;
                case VirtualObjectType.oCMobInter:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobInteractable);
                    break;
                case VirtualObjectType.oCMobBed:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobBed);
                    break;
                case VirtualObjectType.oCMobWheel:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobWheel);
                    break;
                case VirtualObjectType.oCMobSwitch:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSwitch);
                    break;
                case VirtualObjectType.oCMobDoor:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobDoor);
                    break;
                case VirtualObjectType.oCMobContainer:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobContainer);
                    break;
                case VirtualObjectType.oCMobLadder:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobLadder);
                    break;
                case VirtualObjectType.zCVobAnimate:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobAnimate);
                    break;
                default:
                    return new GameObject(name);
            }

            go.name = name;

            // Fill Property data into prefab here
            // Can also be outsourced to a proper method if it becomes a lot.
            go.GetComponent<VobProperties>().SetData(vob);

            return go;
        }

        private static void AddToMobInteractableList(IVirtualObject vob, GameObject go)
        {
            if (go == null)
            {
                return;
            }

            switch (vob.Type)
            {
                case VirtualObjectType.oCMOB:
                case VirtualObjectType.oCMobFire:
                case VirtualObjectType.oCMobInter:
                case VirtualObjectType.oCMobBed:
                case VirtualObjectType.oCMobDoor:
                case VirtualObjectType.oCMobContainer:
                case VirtualObjectType.oCMobSwitch:
                case VirtualObjectType.oCMobWheel:
                    var propertiesComponent = go.GetComponent<VobProperties>();

                    if (propertiesComponent == null)
                    {
                        Debug.LogError($"VobProperties component missing on {go.name} ({vob.Type})");
                    }

                    GameData.VobsInteractable.Add(go.GetComponent<VobProperties>());
                    break;
            }
        }

        private static void CreateParentVobObjectTeleport(GameObject root)
        {
            var allTypes = (VirtualObjectType[])Enum.GetValues(typeof(VirtualObjectType));
            foreach (var type in allTypes.Except(_nonTeleportTypes))
            {
                var newGo = new GameObject(type.ToString());
                newGo.SetParent(root);

                _parentGosTeleport.Add(type, newGo);
            }
        }

        /// <summary>
        /// As PxVobType.PxVob_oCItem get Grabbable Component, they already own a Collider
        /// AND we don't want to teleport on top of them. We therefore exclude them from being added to Teleporter.
        /// </summary>
        private static void CreateParentVobObjectNonTeleport(GameObject root)
        {
            var allTypes = (VirtualObjectType[])Enum.GetValues(typeof(VirtualObjectType));
            foreach (var type in allTypes.Intersect(_nonTeleportTypes))
            {
                var newGo = new GameObject(type.ToString());
                newGo.SetParent(root);

                _parentGosNonTeleport.Add(type, newGo);
            }
        }

        /// <summary>
        /// Render item inside GameObject
        /// </summary>
        public static void CreateItem(int itemId, GameObject go)
        {
            var item = VmInstanceManager.TryGetItemData(itemId);

            CreateItemMesh(item, go);
        }

        public static void CreateItem(int itemId, string spawnpoint, GameObject go)
        {
            var item = VmInstanceManager.TryGetItemData(itemId);

            var position = WayNetHelper.GetWayNetPoint(spawnpoint).Position;

            CreateItemMesh(item, go, position);
        }

        public static void CreateItem(string itemName, GameObject go)
        {
            if (itemName == "")
            {
                return;
            }

            var item = VmInstanceManager.TryGetItemData(itemName);

            CreateItemMesh(item, go);
        }

        [CanBeNull]
        private static GameObject CreateItem(Item vob, GameObject parent = null)
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
            var vobObj = CreateItemMesh(vob, item, prefabInstance, parent);

            if (vobObj == null)
            {
                Object.Destroy(prefabInstance); // No mesh created. Delete the prefab instance again.
                Debug.LogError(
                    $"There should be no! object which can't be found n:{vob.Name} i:{vob.Instance}. We need to use >PxVobItem.instance< to do it right!");
                return null;
            }

            vobObj.GetComponent<VobItemProperties>().SetData(vob, item);

            GuzContext.InteractionAdapter.AddItemComponent(vobObj);

            return vobObj;
        }

        [CanBeNull]
        private static GameObject CreateLight(Light vob, GameObject parent = null)
        {
            if (vob.LightStatic)
            {
                return null;
            }

            var vobObj = new GameObject($"{vob.LightType} Light {vob.Name}");
            vobObj.SetParent(parent ?? _parentGosTeleport[vob.Type], true, true);
            SetPosAndRot(vobObj, vob.Position, vob.Rotation);

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
        private static GameObject CreateMobContainer(Container vob, GameObject parent = null)
        {
            var vobObj = CreateDefaultMesh(vob, parent);

            if (vobObj == null)
            {
                Debug.LogWarning($"{vob.Name} - mesh for MobContainer not found.");
                return null;
            }

            var lootComp = vobObj.AddComponent<DemoContainerLoot>();
            lootComp.SetContent(vob.Contents);

            return vobObj;
        }

        // FIXME - change values for AudioClip based on Sfx and vob value (value overloads itself)
        [CanBeNull]
        private static GameObject CreateSound(Sound vob, GameObject parent = null)
        {
            var go = GetPrefab(vob);
            go.name = $"{vob.SoundName}";
            
            // We don't want to have sound when we boot the game async for 30 seconds in non-spatial blend mode.
            go.SetActive(false);
            go.SetParent(parent ?? _parentGosNonTeleport[vob.Type], true, true);
            SetPosAndRot(go, vob.Position, vob.Rotation);

            var source = go.GetComponent<AudioSource>();

            PrepareAudioSource(source, vob);
            source.clip = VobHelper.GetSoundClip(vob.SoundName);

            go.GetComponent<VobSoundProperties>().SoundData = vob;
            go.GetComponent<SoundHandler>().PrepareSoundHandling();

            return go;
        }

        /// <summary>
        /// FIXME - add specific daytime logic!
        /// Creating AudioSource from PxVobSoundDaytimeData is very similar to PxVobSoundData one.
        /// There are only two differences:
        ///     1. This one has two AudioSources
        ///     2. The sources will be toggled during gameplay when start/end time is reached.
        /// </summary>
        [CanBeNull]
        private static GameObject CreateSoundDaytime(SoundDaytime vob, GameObject parent = null)
        {
            var go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSoundDaytime);
            go.name = $"{vob.SoundName}-{vob.SoundNameDaytime}";
            
            // We don't want to have sound when we boot the game async for 30 seconds in non-spatial blend mode.
            go.SetActive(false);
            go.SetParent(parent ?? _parentGosNonTeleport[vob.Type], true, true);
            SetPosAndRot(go, vob.Position, vob.Rotation);

            var sources = go.GetComponents<AudioSource>();

            PrepareAudioSource(sources[0], vob);
            sources[0].clip = VobHelper.GetSoundClip(vob.SoundName);

            PrepareAudioSource(sources[1], vob);
            sources[1].clip = VobHelper.GetSoundClip(vob.SoundNameDaytime);

            go.GetComponent<VobSoundDaytimeProperties>().SoundDaytimeData = vob;
            go.GetComponent<SoundDaytimeHandler>().PrepareSoundHandling();

            return go;
        }

        private static void PrepareAudioSource(AudioSource source, Sound soundData)
        {
            source.maxDistance = soundData.Radius / 100f; // Gothic's values are in cm, Unity's in m.
            source.volume = soundData.Volume / 100f; // Gothic's volume is 0...100, Unity's is 0...1.

            // Random sounds shouldn't play initially, but after certain time.
            source.playOnAwake = soundData.InitiallyPlaying && soundData.Mode != SoundMode.Random;
            source.loop = soundData.Mode == SoundMode.Loop;
            source.spatialBlend = soundData.Ambient3d ? 1f : 0f;
        }

        private static GameObject CreateZoneMusic(ZoneMusic vob, GameObject parent = null)
        {
            var go = ResourceLoader.TryGetPrefabObject(PrefabType.VobMusic);
            go.SetParent(parent ?? _parentGosNonTeleport[vob.Type], true, true);
            go.name = vob.Name;

            go.layer = Constants.IgnoreRaycastLayer;

            var min = vob.BoundingBox.Min.ToUnityVector();
            var max = vob.BoundingBox.Max.ToUnityVector();

            go.transform.position = (min + max) / 2f;
            go.transform.localScale = max - min;

            go.GetComponent<VobMusicProperties>().MusicData = vob;

            return go;
        }

        private static GameObject CreateTriggerChangeLevel(TriggerChangeLevel vob, GameObject parent = null)
        {
            var vobObj = new GameObject(vob.Name);
            vobObj.SetParent(parent ?? _parentGosTeleport[vob.Type], true, true);

            vobObj.layer = Constants.IgnoreRaycastLayer;

            var trigger = vobObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;

            var min = vob.BoundingBox.Min.ToUnityVector();
            var max = vob.BoundingBox.Max.ToUnityVector();
            vobObj.transform.position = (min + max) / 2f;

            vobObj.transform.localScale = max - min;

            var triggerHandler = vobObj.AddComponent<ChangeLevelTriggerHandler>();
            triggerHandler.LevelName = vob.LevelName;
            triggerHandler.StartVob = vob.StartVob;
            return vobObj;
        }

        /// <summary>
        /// Basically a free point where NPCs can do something like sitting on a bench etc.
        /// @see for more information: https://ataulien.github.io/Inside-Gothic/objects/spot/
        /// </summary>
        private static GameObject CreateSpot(IVirtualObject vob, GameObject parent = null, bool debugDraw = false)
        {
            // FIXME - change to a Prefab in the future.
            var vobObj = GetPrefab(vob);

            if (!debugDraw)
            {
                // Quick win: If we don't want to render the spots, we just remove the Renderer.
                Object.Destroy(vobObj.GetComponent<MeshRenderer>());
            }

            var fpName = vob.Name.IsEmpty() ? "START" : vob.Name;
            vobObj.name = fpName;
            vobObj.SetParent(parent ?? _parentGosTeleport[vob.Type], true, true);

            var freePointData = new FreePoint
            {
                Name = fpName,
                Position = vob.Position.ToUnityVector(),
                Direction = vob.Rotation.ToUnityQuaternion().eulerAngles
            };
            vobObj.GetComponent<VobSpotProperties>().Fp = freePointData;
            GameData.FreePoints.Add(fpName, freePointData);

            SetPosAndRot(vobObj, vob.Position, vob.Rotation);
            return vobObj;
        }

        private static GameObject CreateLadder(IVirtualObject vob, GameObject parent = null)
        {
            var vobObj = CreateDefaultMesh(vob, parent, true);

            return vobObj;
        }

        // FIXME - Needs to be outsourced to Context.InteractionAdapter as we have different handling for XRIT and HVR.
        private static GameObject CreateSeat(IVirtualObject vob, GameObject parent = null)
        {
            //to be used for creating chairs, benches etc
            //based on Creating Ladder
            var vobObj = CreateDefaultMesh(vob);
            var meshColliderComp = vobObj.GetComponentInChildren<MeshCollider>();

            var grabComp = meshColliderComp.gameObject.AddComponent<XRGrabInteractable>();
            var rigidbodyComp = meshColliderComp.gameObject.GetComponent<Rigidbody>();

            var seat = meshColliderComp.gameObject.AddComponent<Seat>();

            meshColliderComp.convex = true;

            rigidbodyComp.isKinematic = true;
            grabComp.throwOnDetach = false;
            grabComp.trackPosition = false;
            grabComp.trackRotation = false;

            return vobObj;
        }

        private static GameObject CreateItemMesh(Item vob, ItemInstance item, GameObject go, GameObject parent = null)
        {
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            return MeshFactory.CreateVob(item.Visual, mrm, vob.Position.ToUnityVector(),
                vob.Rotation.ToUnityQuaternion(),
                true, parent ?? _parentGosNonTeleport[vob.Type], go, false);
        }

        private static GameObject CreateItemMesh(ItemInstance item, GameObject parentGo,
            UnityEngine.Vector3 position = default)
        {
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            return MeshFactory.CreateVob(item.Visual, mrm, position, default, false, parentGo, useTextureArray: false);
        }

        private static GameObject CreateDecal(IVirtualObject vob, GameObject parent = null)
        {
            return MeshFactory.CreateVobDecal(vob, (VisualDecal)vob.Visual, parent ?? _parentGosTeleport[vob.Type]);
        }

        /// <summary>
        /// Please check description at worldofgothic for more details:
        /// https://www.worldofgothic.de/modifikation/index.php?go=particelfx
        /// </summary>
        private static GameObject CreatePfx(IVirtualObject vob, GameObject parent = null)
        {
            var pfxGo = ResourceLoader.TryGetPrefabObject(PrefabType.VobPfx);
            pfxGo.name = vob.Visual!.Name;

            // if parent exists then set rotation before parent (for correct rotation vob trees)
            if (parent)
            {
                SetPosAndRot(pfxGo, vob.Position, vob.Rotation);
                pfxGo.SetParent(parent, true);
            }
            else
            {
                pfxGo.SetParent(parent ?? _parentGosTeleport[vob.Type], true, true);
                SetPosAndRot(pfxGo, vob.Position, vob.Rotation);
            }

            var pfx = VmInstanceManager.TryGetPfxData(vob.Visual.Name);
            var particleSystem = pfxGo.GetComponent<ParticleSystem>();

            pfxGo.GetComponent<VobPfxProperties>().PfxData = pfx;

            particleSystem.Stop();

            var gravity = pfx.FlyGravityS.Split();
            float gravityX = 1f, gravityY = 1f, gravityZ = 1f;
            if (gravity.Length == 3)
            {
                // Gravity seems too low. Therefore *10k.
                gravityX = float.Parse(gravity[0], CultureInfo.InvariantCulture) * 10000;
                gravityY = float.Parse(gravity[1], CultureInfo.InvariantCulture) * 10000;
                gravityZ = float.Parse(gravity[2], CultureInfo.InvariantCulture) * 10000;
            }

            // Main module
            {
                var mainModule = particleSystem.main;
                // I assume we need to change milliseconds to seconds.
                var minLifeTime = (pfx.LspPartAvg - pfx.LspPartVar) / 1000;
                var maxLifeTime = (pfx.LspPartAvg + pfx.LspPartVar) / 1000;
                mainModule.duration = 1f; // I assume pfx data wants a cycle being 1 second long.
                mainModule.startLifetime = new ParticleSystem.MinMaxCurve(minLifeTime, maxLifeTime);
                mainModule.loop = Convert.ToBoolean(pfx.PpsIsLooping);

                var minSpeed = (pfx.VelAvg - pfx.VelVar) / 1000;
                var maxSpeed = (pfx.VelAvg + pfx.VelVar) / 1000;
                mainModule.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            }

            // Emission module
            {
                var emissionModule = particleSystem.emission;
                emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(pfx.PpsValue);
            }

            // Force over Lifetime module
            {
                var forceModule = particleSystem.forceOverLifetime;
                if (gravity.Length == 3)
                {
                    forceModule.enabled = true;
                    forceModule.x = gravityX;
                    forceModule.y = gravityY;
                    forceModule.z = gravityZ;
                }
            }

            // Color over Lifetime module
            {
                var colorOverTime = particleSystem.colorOverLifetime;
                colorOverTime.enabled = true;
                var gradient = new Gradient();
                var colorStart = pfx.VisTexColorStartS.Split();
                var colorEnd = pfx.VisTexColorEndS.Split();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new(
                            new Color(float.Parse(colorStart[0]) / 255, float.Parse(colorStart[1]) / 255,
                                float.Parse(colorStart[2]) / 255),
                            0f),
                        new(
                            new Color(float.Parse(colorEnd[0]) / 255, float.Parse(colorEnd[1]) / 255,
                                float.Parse(colorEnd[2]) / 255), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new(pfx.VisAlphaStart / 255, 0),
                        new(pfx.VisAlphaEnd / 255, 1)
                    });
                colorOverTime.color = gradient;
            }

            // Size over lifetime module
            {
                var sizeOverTime = particleSystem.sizeOverLifetime;
                sizeOverTime.enabled = true;

                var curve = new AnimationCurve();
                var shapeScaleKeys = pfx.ShpScaleKeysS.Split();
                if (shapeScaleKeys.Length > 1 && !pfx.ShpScaleKeysS.IsEmpty())
                {
                    var curveTime = 0f;

                    foreach (var key in shapeScaleKeys)
                    {
                        curve.AddKey(curveTime, float.Parse(key) / 100 * float.Parse(pfx.ShpDimS));
                        curveTime += 1f / shapeScaleKeys.Length;
                    }

                    sizeOverTime.size = new ParticleSystem.MinMaxCurve(1f, curve);
                }
            }

            // Renderer module
            {
                var rendererModule = pfxGo.GetComponent<ParticleSystemRenderer>();
                // FIXME - Move to a cached constant value
                var standardShader = Constants.ShaderUnlitParticles;
                var material = new Material(standardShader);
                rendererModule.material = material;
                GameGlobals.Textures.SetTexture(pfx.VisNameS, rendererModule.material);
                // renderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest; // First check with no change.

                switch (pfx.VisAlphaFuncS.ToUpper())
                {
                    case "BLEND":
                        rendererModule.material.ToTransparentMode(); // e.g. leaves.pfx.
                        break;
                    case "ADD":
                        rendererModule.material.ToAdditiveMode();
                        break;
                    default:
                        Debug.LogWarning($"Particle AlphaFunc {pfx.VisAlphaFuncS} not yet handled.");
                        break;
                }

                // makes the material render both faces
                rendererModule.material.SetInt("_Cull", (int)CullMode.Off);

                switch (pfx.VisOrientationS)
                {
                    case "NONE":
                        rendererModule.alignment = ParticleSystemRenderSpace.View;
                        break;
                    case "WORLD":
                        rendererModule.alignment = ParticleSystemRenderSpace.World;
                        break;
                    case "VELO":
                        rendererModule.alignment = ParticleSystemRenderSpace.Velocity;
                        break;
                    default:
                        Debug.LogWarning($"visOrientation {pfx.VisOrientationS} not yet handled.");
                        break;
                }
            }

            // Shape module
            {
                var shapeModule = particleSystem.shape;
                switch (pfx.ShpTypeS.ToUpper())
                {
                    case "SPHERE":
                        shapeModule.shapeType = ParticleSystemShapeType.Sphere;
                        break;
                    case "CIRCLE":
                        shapeModule.shapeType = ParticleSystemShapeType.Circle;
                        break;
                    case "MESH":
                        shapeModule.shapeType = ParticleSystemShapeType.Mesh;
                        break;
                    default:
                        Debug.LogWarning($"Particle ShapeType {pfx.ShpTypeS} not yet handled.");
                        break;
                }

                var shapeDimensions = pfx.ShpDimS.Split();
                switch (shapeDimensions.Length)
                {
                    case 1:
                        // cm in m
                        shapeModule.radius = float.Parse(shapeDimensions[0], CultureInfo.InvariantCulture) / 100;
                        break;
                    default:
                        Debug.LogWarning($"shpDim >{pfx.ShpDimS}< not yet handled");
                        break;
                }

                shapeModule.rotation = new UnityEngine.Vector3(pfx.DirAngleElev, 0, 0);

                var shapeOffsetVec = pfx.ShpOffsetVecS.Split();
                if (float.TryParse(shapeOffsetVec[0], out var x) && float.TryParse(shapeOffsetVec[1], out var y) &&
                    float.TryParse(shapeOffsetVec[2], out var z))
                {
                    shapeModule.position = new UnityEngine.Vector3(x / 100, y / 100, z / 100);
                }

                shapeModule.alignToDirection = true;

                shapeModule.radiusThickness = Convert.ToBoolean(pfx.ShpIsVolume) ? 1f : 0f;
            }

            particleSystem.Play();

            return pfxGo;
        }

        private static GameObject CreateAnimatedVob(Animate vob, GameObject parent = null)
        {
            var go = CreateDefaultMesh(vob, parent, true);
            var morph = go.AddComponent<VobAnimateMorph>();
            morph.StartAnimation(vob.Visual!.Name);
            return go;
        }

        private static GameObject CreateNpc(ZenKit.Vobs.Npc vob)
        {
            var npcSymbol = GameData.GothicVm.GetSymbolByName(vob.Name);
            var newNpc = NpcCreator.InitializeNpc(npcSymbol.Index);

            if (newNpc == null)
            {
                return null;
            }

            NpcCreator.SetSpawnPoint(newNpc, vob.Position.ToUnityVector(), vob.Rotation.ToUnityQuaternion());
            GameGlobals.NpcMeshCulling.AddCullingEntry(newNpc);

            var loadedRoutineSymbol = GameData.GothicVm.GetSymbolByName(vob.CurrentRoutine);
            var properties = newNpc.GetComponent<NpcProperties>();

            // If we get an NPC from VOBTree, it means the NPC was very close/visible when saving the game.
            // Inside Vob, we have the information of the last executed routine. Let's exchange it instead having the initial one.
            NpcHelper.ExchangeRoutine(newNpc, properties.NpcInstance, loadedRoutineSymbol.Index);

            return newNpc;
        }

        private static GameObject CreateDefaultMesh(IVirtualObject vob, GameObject parent = null,
            bool nonTeleport = false)
        {
            var parentGo = nonTeleport ? _parentGosNonTeleport[vob.Type] : _parentGosTeleport[vob.Type];
            var meshName = vob.ShowVisual ? vob.Visual!.Name : vob.Name;

            if (meshName.IsEmpty())
            {
                return null;
            }

            var go = GetPrefab(vob);

            // MDL
            var mdl = ResourceLoader.TryGetModel(meshName);
            if (mdl != null)
            {
                var ret = MeshFactory.CreateVob(meshName, mdl, vob.Position.ToUnityVector(),
                    vob.Rotation.ToUnityQuaternion(), parent ?? parentGo, go);

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
                var ret = MeshFactory.CreateVob(meshName, mdm, mdh, vob.Position.ToUnityVector(),
                    vob.Rotation.ToUnityQuaternion(), parent ?? parentGo, go);

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
                var ret = MeshFactory.CreateVob(meshName, mmb, vob.Position.ToUnityVector(),
                    vob.Rotation.ToUnityQuaternion(), parent ?? parentGo, go);

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

                var ret = MeshFactory.CreateVob(meshName, mrm, vob.Position.ToUnityVector(),
                    vob.Rotation.ToUnityQuaternion(), withCollider, parent ?? parentGo, go);

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

        private static void SetPosAndRot(GameObject obj, Vector3 position, Matrix3x3 rotation)
        {
            SetPosAndRot(obj, position.ToUnityVector(), rotation.ToUnityQuaternion());
        }

        private static void SetPosAndRot(GameObject obj, UnityEngine.Vector3 position, Quaternion rotation)
        {
            obj.transform.SetLocalPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// Some object are kind of null. We have Position only. this method is to compare with Gothic Spacer and remove if not needed later.
        /// </summary>
        private static GameObject CreateDebugObject(IVirtualObject vob, GameObject parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"{vob.Name} - Empty DEBUG object. Check with Spacer if buggy.";
            SetPosAndRot(go, vob.Position, vob.Rotation);
            return go;
        }
    }
}
