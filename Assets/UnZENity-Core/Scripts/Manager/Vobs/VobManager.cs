using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Creator.Meshes.Builder;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.LoadingBars;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using GUZ.Core.Vob;
using MyBox;
using UnityEngine;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;
using Object = UnityEngine.Object;

namespace GUZ.Core.Manager.Vobs
{
    public class VobManager
    {
        private const string _noSoundName = "nosound.wav";

        // Supporter class where the whole Init() logic is outsourced for better readability.
        private VobInitializer _initializer = new ();

        private Dictionary<VirtualObjectType, GameObject> _vobTypeParentGOs = new();
        private List<GameObject> _cullingVobObjects = new();
        private Queue<VobLoader> _objectsToInitQueue = new();

        // Important: All of them are not culled!
        private static readonly VirtualObjectType[] _vobTypesNonLazyLoading =
        {
            // FIXME - As we want to cull these two, we can simply lazyLoad them
            // VirtualObjectType.zCVobSound,
            // VirtualObjectType.zCVobSoundDaytime,
            VirtualObjectType.oCZoneMusic,
            VirtualObjectType.oCZoneMusicDefault,
            VirtualObjectType.zCVobSpot,
            VirtualObjectType.zCVobStartpoint,
            VirtualObjectType.oCTriggerChangeLevel,
            VirtualObjectType.zCMover,
            VirtualObjectType.zCPFXController,
            VirtualObjectType.zCTriggerList,
            VirtualObjectType.zCVobLevelCompo
        };

        public void Init(ICoroutineManager coroutineManager)
        {
            coroutineManager.StartCoroutine(InitVobCoroutine());
        }

        /// <summary>
        /// Load VOBs during world creation. The VOBs itself are then lazy loaded (i.e. when Culling kicks in, a Loading component
        /// will take care of initializing later) or some of them are loaded immediately (e.g. Spots).
        /// </summary>
        public async Task CreateWorldVobsAsync(DeveloperConfig config, LoadingManager loading, List<IVirtualObject> vobs, GameObject root)
        {
            PreCreateWorldVobs(vobs, root, loading);
            await CreateWorldVobs(config, loading, vobs);

            // Ensure all Vob skeletons are created.
            await Task.Yield();

            PostCreateWorldVobs();
        }

        public GameObject GetRootGameObjectOfType(VirtualObjectType type)
        {
            if (_vobTypeParentGOs.TryGetValue(type, out var parentGo))
            {
                return parentGo;
            }
            else
            {
                Logger.LogError($"No suitable root GO found for type >{type}<", LogCat.Vob);
                return null;
            }
        }

        /// <summary>
        /// Some VOBs are initialized eagerly (e.g. when there is no performance benefit in doing so later or its needed directly).
        /// </summary>
        public GameObject InitVobNow(IVirtualObject vob, GameObject parent)
        {
            return _initializer.InitVob(vob, parent, default, true);
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
            
            // Do not add elements to be loaded twice.
            if (_objectsToInitQueue.Contains(loaderComp))
                return;

            _objectsToInitQueue.Enqueue(go.GetComponent<VobLoader>());
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

        // DEBUG - Check how many frames it took to initialize all the objects
        // private int firstFrameQueueFilledUp;

        private IEnumerator InitVobCoroutine()
        {
            while (true)
            {
                if (_objectsToInitQueue.IsEmpty())
                {
                    // DEBUG
                    // if (firstFrameQueueFilledUp != 0)
                    // {
                    //     Logger.LogWarning($"It took {Time.frameCount - firstFrameQueueFilledUp} frames to clear the queue.");
                    //     firstFrameQueueFilledUp = 0;
                    // }
                    yield return null;
                }
                else
                {
                    // DEBUG
                    // if (firstFrameQueueFilledUp == 0)
                    // {
                    //     firstFrameQueueFilledUp = Time.frameCount;
                    // }

                    var item = _objectsToInitQueue.Dequeue();
                    
                    item.IsLoaded = true;

                    // We assume that each loaded VOB is centered at parent=0,0,0.
                    // Should work smoothly until we start lazy loading sub-vobs ;-)
                    _initializer.InitVob(item.Vob, item.gameObject, default, true);

                    yield return FrameSkipper.TrySkipToNextFrameCoroutine();
                }
            }
        }

        public void CreateItemMesh(int itemId, string spawnPoint)
        {
            var item = VmInstanceManager.TryGetItemData(itemId);

            var position = WayNetHelper.GetWayNetPoint(spawnPoint).Position;

            _initializer.CreateItemMesh(item, GetRootGameObjectOfType(VirtualObjectType.oCItem), position);
        }

        /// <summary>
        /// Create item with mesh only. No special handling like grabbing etc.
        /// e.g. used for NPCs drinking beer mesh in their hand.
        /// </summary>
        public void CreateItemMesh(int itemId, GameObject parentGo)
        {
            if (itemId == -1)
            {
                Logger.LogError("No ItemId found. Is this a bug on daedalus or our side?", LogCat.Vob);
                return; // no item
            }
            var item = VmInstanceManager.TryGetItemData(itemId);

            _initializer.CreateItemMesh(item, parentGo, default);
        }

        /// <summary>
        /// Create item with mesh only. No special handling like grabbing etc.
        /// e.g. used for NPCs drinking beer mesh in their hand.
        /// </summary>
        public void CreateItemMesh(string itemName, GameObject parentGo)
        {
            if (itemName == "")
            {
                return;
            }

            var item = VmInstanceManager.TryGetItemData(itemName);

            _initializer.CreateItemMesh(item, parentGo, default);
        }

        /// <summary>
        /// To save memory, we can also Destroy Vobs and their Mesh+GO structure.
        /// </summary>
        public void DestroyVob(GameObject go)
        {
            throw new NotImplementedException();
        }
        
        private void PreCreateWorldVobs(List<IVirtualObject> vobs, GameObject rootGo, LoadingManager loading)
        {
            loading.SetPhase(nameof(WorldLoadingBarHandler.ProgressType.VOB), GetTotalVobCount(vobs));

            _cullingVobObjects.Clear();

            // We reset the GO dictionary.
            _vobTypeParentGOs = new();

            // Create root VOB GOs.
            var allTypes = (VirtualObjectType[])Enum.GetValues(typeof(VirtualObjectType));
            foreach (var type in allTypes)
            {
                var newGo = new GameObject(type.ToString());
                newGo.SetParent(rootGo);

                _vobTypeParentGOs[type] = newGo;
            }
        }

        /// <summary>
        /// Hint: The calculation is somewhat off: When we set a VOB to be lazy loaded, we will not calculate its children as "created".
        ///       Therefore, the loading bar will "hop" at the end, as we didn't calculate correctly. But it causes no harm.
        /// </summary>
        private int GetTotalVobCount(List<IVirtualObject> vobs)
        {
            return vobs.Count + vobs.Sum(vob => GetTotalVobCount(vob.Children));
        }

        private void PostCreateWorldVobs()
        {
            GameGlobals.VobMeshCulling.PrepareVobCulling(_cullingVobObjects);

            // DEBUG - If we want to load all VOBs at once, we need to initialize all LazyLoad objects now.
            if (!GameGlobals.Config.Dev.EnableVOBMeshCulling)
            {
                var lazyLoadVobs = Object.FindObjectsOfType<VobLoader>(true);
                lazyLoadVobs.ForEach(i => GameGlobals.Vobs.InitVob(i.gameObject));
            }
        }

        private async Task CreateWorldVobs(DeveloperConfig config, LoadingManager loading, List<IVirtualObject> vobs)
        {
            foreach (var vob in vobs)
            {
                // It's simpler to have both of them in here.
                loading.Tick();
                await FrameSkipper.TrySkipToNextFrame();

                PreLoadVob(vob);
                
                switch (vob.Type)
                {
                    // A LevelCompo contains no data. Simply check its children.
                    case VirtualObjectType.zCVobLevelCompo:
                        await CreateWorldVobs(config, loading, vob.Children);
                        continue;
                    case VirtualObjectType.oCNpc:
                        GameGlobals.Npcs.CreateVobNpc((ZenKit.Vobs.Npc)vob);
                        continue;
                }

                // If our VOB type is ignored by Dev config, skip it and its children.
                if (!config.SpawnVOBTypes.Value.IsEmpty() && !config.SpawnVOBTypes.Value.Contains(vob.Type))
                {
                    continue;
                }

                var vobContainer = new VobContainer(vob);
                MultiTypeCache.VobCache.Add(vobContainer);

                if (_vobTypesNonLazyLoading.Contains(vob.Type))
                {
                    vobContainer.Go = CreateVobNow(vob);
                }
                else
                {
                    vobContainer.Go = CreateVobLazily(vob);

                    // We assume that all VOBs with meshes are lazy loaded only.
                    AddToMobInteractableList(vobContainer);
                }
            }
        }

        private void PreLoadVob(IVirtualObject vob)
        {
        }

        /// <summary>
        /// Eager loading a VOB simply means we call the Init() method immediately.
        /// </summary>
        private GameObject CreateVobNow(IVirtualObject vob)
        {
            return InitVobNow(vob, GetRootGameObjectOfType(vob.Type));
        }

        public GameObject CreateItem(Item item)
        {
            // We need to call the LazyLoad creation to have VobLoader.component added (as normal Items in the world)
            var go = CreateVobLazily(item);

            // We initialize the object directly, though.
            var loader = go.GetComponent<VobLoader>();
            loader.IsLoaded = true;

            _initializer.InitVob(loader.Vob, loader.gameObject, default, true);

            return go;
        }
        
        /// <summary>
        /// When we Lazy Load a VOB, we add a component which stores initialization data.
        /// Once Culling fetches the object, we will read this data and call InitVob() later.
        /// </summary>
        private GameObject CreateVobLazily(IVirtualObject vob)
        {
            // Skip disabled features.
            switch (vob.Visual!.Type)
            {
                case VisualType.Decal:
                    // Skip object
                    if (!GameGlobals.Config.Dev.EnableDecalVisuals)
                    {
                        return null;
                    }
                    break;
                case VisualType.ParticleEffect:
                    // Skip object
                    if (!GameGlobals.Config.Dev.EnableParticleEffects)
                    {
                        return null;
                    }
                    break;
            }

            // Non-static lights aren't handled so far.
            if (vob.Type == VirtualObjectType.zCVobLight && !((ILight)vob).LightStatic)
            {
                return null;
            }

            var go = new GameObject(vob.GetVisualName());
            var loader = go.AddComponent<VobLoader>();
            loader.Vob = vob;

            _initializer.SetPosAndRot(go, vob.Position, vob.Rotation);
            go.SetParent(GetRootGameObjectOfType(vob.Type));

            _cullingVobObjects.Add(go);

            return go;
        }

        private static void AddToMobInteractableList(VobContainer container)
        {
            if (container.Go == null)
                return;

            switch (container.Vob.Type)
            {
                // case VirtualObjectType.oCMOB: // FIXME - Needed? e.g. IMovableObject
                case VirtualObjectType.oCMobFire:
                case VirtualObjectType.oCMobInter:
                case VirtualObjectType.oCMobBed:
                case VirtualObjectType.oCMobDoor:
                case VirtualObjectType.oCMobContainer:
                case VirtualObjectType.oCMobSwitch:
                case VirtualObjectType.oCMobWheel:
                    var visualScheme = container.Vob.Visual?.Name.Split('_').First().ToUpper(); // e.g. BED_1_OC.ASC => BED);

                    if (visualScheme.IsNullOrEmpty())
                        return;
                    
                    GameData.VobsInteractable.TryAdd(visualScheme, new());
                    GameData.VobsInteractable[visualScheme!].Add(container);
                    break;
            }
        }
    }
}
