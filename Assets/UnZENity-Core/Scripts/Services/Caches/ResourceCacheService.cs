using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DirectMusic;
using GUZ.Core.Extensions;
using GUZ.Core.Logging;
using GUZ.Core.Models.Caches;
using GUZ.Core.Services.Context;
using JetBrains.Annotations;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;
using Font = ZenKit.Font;
using Logger = GUZ.Core.Logging.Logger;
using Mesh = ZenKit.Mesh;
using Object = UnityEngine.Object;
using Texture = ZenKit.Texture;

namespace GUZ.Core.Services.Caches
{
    public class ResourceCacheService
    {
        [Inject] private readonly ContextInteractionService _contextInteractionService;
        
        private readonly Vfs _vfs = new();
        private readonly Loader _dmLoader = Loader.Create(LoaderOptions.Default | LoaderOptions.Download);

        private ResourceCacheType<ZenKit.World> _world;
        private ResourceCacheType<IModelScript> _modelScript;
        private ResourceCacheType<IModelAnimation> _modelAnimation;
        private ResourceCacheType<IMesh> _mesh;
        private ResourceCacheType<IModel> _model;
        private ResourceCacheType<IModelHierarchy> _modelHierarchy;
        private ResourceCacheType<IModelMesh> _modelMesh;
        private ResourceCacheType<IMultiResolutionMesh> _multiResolutionMesh;
        private ResourceCacheType<IMorphMesh> _morphMesh;
        private ResourceCacheType<IFont> _font;
        private ResourceCacheType<GameObject> _prefab;

        public void Init(string root)
        {
            var workPath = FindWorkPath(root);
            var diskPaths = FindDiskPaths(root);

            diskPaths.ForEach(v => _vfs.MountDisk(v, VfsOverwriteBehavior.Older));
            _vfs.Mount(Path.GetFullPath(workPath), "/_work", VfsOverwriteBehavior.Older);

            _dmLoader.AddResolver(name =>
            {
                var node = _vfs.Find(name);
                return node?.Buffer.Bytes;
            });
            
            _world = new(
                (s, version) =>
                {
                    // We do not cache the world itself, but only the loading pointer. As Mesh could exhaust our memory consumption.
                    var fireWorld = new ZenKit.World(_vfs, s, version);

                    // FIRE worlds aren't positioned at 0,0,0. We need to do it now, to have the correct parent-child positioning.
                    fireWorld.RootObjects.ForEach(i => i.Position = default);

                    return fireWorld;
                }
            );
            
            _modelScript = new(s => new ModelScript(_vfs, s).Cache());
            _model = new(s => new ZenKit.Model(_vfs, s).Cache());
            _modelAnimation = new(s => new ModelAnimation(_vfs, s).Cache());
            _mesh = new(s => new Mesh(_vfs, s).Cache());
            _modelHierarchy = new(s => new ModelHierarchy(_vfs, s).Cache() );
            _modelMesh = new(s => new ModelMesh(_vfs, s).Cache());
            _multiResolutionMesh = new(s => new MultiResolutionMesh(_vfs, s).Cache());
            _morphMesh = new(s => new MorphMesh(_vfs, s).Cache());
            _font = new(s => new Font(_vfs, s).Cache());
            
            _prefab = new(s =>
            {
                // Lookup is done in following places:
                // 1. CONTEXT_NAME/Prefabs/... - overwrites lookup path below, used for specific prefabs, for current context (HVR, Flat, ...)
                // 2. Prefabs/... - Located inside core module (UnZENity-Core), if we don't need special handling.
                var contextPrefixPath = $"{_contextInteractionService.GetContextName()}/{s}";
                return new[] { contextPrefixPath, s }.Select(Resources.Load<GameObject>)
                    .FirstOrDefault(newPrefab => newPrefab != null);
            });
        }

        [CanBeNull]
        public ITexture TryGetTexture([NotNull] string key)
        {
            // Zen texture data is not cached as we do not need to keep the pixel data in memory as managed objects.
            // Instead, TextureCache loads the texture data when need and creates a Texture2D.
            try
            {
                return new Texture(_vfs, $"{GetPreparedKey(key)}-c.tex");
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Once a world is loaded, release data which was cached during loading. We don't need it any longer and free up memory.
        /// </summary>
        public void ReleaseLoadedData()
        {
            _mesh.Dispose();
            _model.Dispose();
            _modelMesh.Dispose();
            _multiResolutionMesh.Dispose();
        }

        [CanBeNull]
        public IModelScript TryGetModelScript([NotNull] string key)
        {
            return _modelScript.TryLoad($"{GetPreparedKey(key)}.mds", out var item) ? item : null;
        }

        [CanBeNull]
        public IModelAnimation TryGetModelAnimation([NotNull] string mds, [NotNull] string key)
        {
            key = $"{GetPreparedKey(mds)}-{GetPreparedKey(key)}.man";
            return _modelAnimation.TryLoad(key, out var item) ? item : null;
        }

        [CanBeNull]
        public IMesh TryGetMesh([NotNull] string key)
        {
            return _mesh.TryLoad($"{GetPreparedKey(key)}.msh", out var item) ? item : null;
        }

        [CanBeNull]
        public IModelHierarchy TryGetModelHierarchy([NotNull] string key)
        {
            return _modelHierarchy.TryLoad($"{GetPreparedKey(key)}.mdh", out var item) ? item : null;
        }

        [CanBeNull]
        public IModel TryGetModel([NotNull] string key)
        {
            return _model.TryLoad($"{GetPreparedKey(key)}.mdl", out var item) ? item : null;
        }

        [CanBeNull]
        public IModelMesh TryGetModelMesh([NotNull] string key)
        {
            return _modelMesh.TryLoad($"{GetPreparedKey(key)}.mdm", out var item) ? item : null;
        }

        [CanBeNull]
        public IMultiResolutionMesh TryGetMultiResolutionMesh([NotNull] string key)
        {
            return _multiResolutionMesh.TryLoad($"{GetPreparedKey(key)}.mrm", out var item) ? item : null;
        }

        [CanBeNull]
        public IMorphMesh TryGetMorphMesh([NotNull] string key)
        {
            return _morphMesh.TryLoad($"{GetPreparedKey(key)}.mmb", out var item) ? item : null;
        }

        [CanBeNull]
        public IFont TryGetFont([NotNull] string key)
        {
            return _font.TryLoad($"{GetPreparedKey(key)}.fnt", out var item) ? item : null;
        }

        /// <summary>
        /// Loading of binary .wav data. For internal use within SoundCreator only.
        /// Please consider using SoundCreator.ToAudioClip() instead.
        /// </summary>
        [CanBeNull]
        public byte[] TryGetSoundBytes([NotNull] string key)
        {
            var node = _vfs.Find($"{GetPreparedKey(key)}.wav");
            return node == null ? null : node.Buffer.Bytes;
        }

        [CanBeNull]
        public Segment TryGetSegment([NotNull] string key)
        {
            // NOTE(lmichaelis): There is no caching required here, since the loader
            //                   already caches segments upon loading them
            return _dmLoader.GetSegment(key);
        }

        /// <summary>
        /// Load a <b>new</b> <see cref="DaedalusVm"/> from the assets by name. VMs are not cached, thus this call will
        /// <b>always create a new VM</b>. You most likely need to use the prepared VM instances in
        /// <see cref="GameStateService"/> instead.
        /// </summary>
        [CanBeNull]
        public DaedalusVm TryGetDaedalusVm([NotNull] string key)
        {
            // NOTE(lmichaelis): These are not cached, since they contain internal state
            //                   which should not be shared.
            return new DaedalusVm(_vfs, $"{GetPreparedKey(key)}.dat");
        }

        [CanBeNull]
        public ZenKit.World TryGetWorld([NotNull] string key)
        {
            return new ZenKit.World(_vfs, $"{GetPreparedKey(key)}.zen");
        }

        /// <summary>
        /// We need to be careful to cache only Fire.zen files as they're small and used multiple times on a world.
        /// Therefore by default, we load uncached.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="version"></param>
        /// <param name="cacheFire"></param>
        /// <returns></returns>
        [CanBeNull]
        public ZenKit.World TryGetWorld([NotNull] string key, GameVersion version, bool cacheFire = false)
        {
            if (cacheFire)
            {
                return _world.TryLoad($"{GetPreparedKey(key)}.zen", version, out var item) ? item : null;
            }
            else
            {
                return new ZenKit.World(_vfs, $"{GetPreparedKey(key)}.zen", version);
            }
        }

        [CanBeNull]
        public GameObject TryGetPrefabObject(PrefabType key, string name = null, GameObject parent = null)
        {
            // worldPositionStays=false - initialize the object at 0,0,0. If needed, we will later set positions.

            var go = Object.Instantiate(TryGetPrefab(key), parent?.transform, worldPositionStays: false);
                
            if (name != null)
                go.name = name;

            return go;
        }

        /// <summary>
        /// Alternative way to load a dynamically named prefab and cache it.
        /// 
        /// HINT: Please check if using PrefabType overload is better suited before using this function.
        /// </summary>
        public GameObject TryGetPrefabObject(string prefabPath, string name = null, GameObject parent = null, bool worldPositionStays = true)
        {
            var go = Object.Instantiate(TryGetPrefab(prefabPath), parent?.transform, worldPositionStays);

            if (name != null)
            {
                go.name = name;
            }

            return go;
        }

        [CanBeNull]
        private GameObject TryGetPrefab(PrefabType key)
        {
            return _prefab.TryLoad(key.Path(), out var item) ? item : null;
        }
        
        [CanBeNull]
        private GameObject TryGetPrefab(string prefabPath)
        {
            _prefab.TryLoad(prefabPath, out var item);
            
            if (item == null)
                Logger.LogError($"Prefab at >{prefabPath}< not found.", LogCat.Loading);
            
            return item;
        }

        [NotNull]
        public string GetPreparedKey([NotNull] string key)
        {
            return Path.GetFileNameWithoutExtension(key).ToLower();
        }


        /// <summary>
        /// Determines the absolute path to a Gothic installation's <c>_work</c> directory given the installation's
        /// root directory. It does the lookup case-insensitively.
        /// </summary>
        /// <param name="root">A path to a Gothic installation's root directory</param>
        /// <returns>The absolute path to the <c>_work</c> folder of the Gothic installation</returns>
        private string FindWorkPath(string root)
        {
            var path = Directory.GetDirectories(root, "_work", new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = false
            }).First();

            return Path.GetFullPath(path, root);
        }

        /// <summary>
        /// Finds all <c>.vdf</c> files in a Gothic installation's <c>data</c> directory given the installation's
        /// root directory. It does the lookup case-insensitively.
        /// </summary>
        /// <param name="root">A path to a Gothic installation's root directory</param>
        /// <returns>A list of absolute paths to all <c>.vdf</c> files the <c>data</c> folder of the Gothic installation</returns>
        private List<string> FindDiskPaths(string root)
        {
            var path = Directory.GetDirectories(root, "data", new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = false
            }).First();

            var data = Path.GetFullPath(path, root);
            var files = Directory.GetFiles(data, "*.vdf", new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            });

            return files.Select(v => Path.GetFullPath(v, data)).ToList();
        }
    }
}
