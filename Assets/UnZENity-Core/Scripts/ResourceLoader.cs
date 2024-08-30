using System.Collections.Generic;
using System.IO;
using System.Linq;
using DirectMusic;
using GUZ.Core.Context;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Data;
using GUZ.Core.Globals;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit;
using Font = ZenKit.Font;
using Mesh = ZenKit.Mesh;
using Object = UnityEngine.Object;
using Texture = ZenKit.Texture;

namespace GUZ.Core
{
    public static class ResourceLoader
    {
        private static readonly Vfs _vfs = new();
        private static readonly Loader _dmLoader = Loader.Create(LoaderOptions.Default | LoaderOptions.Download);

        private static readonly Resource<ITexture> _textures = new(
            s => new Texture(_vfs, s).Cache()
        );

        private static readonly Resource<IModelScript> _modelScript = new(
            s => new ModelScript(_vfs, s).Cache()
        );

        private static readonly Resource<IModelAnimation> _modelAnimation = new(
            s => new ModelAnimation(_vfs, s).Cache()
        );

        private static readonly Resource<IMesh> _mesh = new(
            s => new Mesh(_vfs, s).Cache()
        );

        private static readonly Resource<IModelHierarchy> _modelHierarchy = new(
            s => new ModelHierarchy(_vfs, s).Cache()
        );

        private static readonly Resource<IModel> _model = new(
            s => new Model(_vfs, s).Cache()
        );

        private static readonly Resource<IModelMesh> _modelMesh = new(
            s => new ModelMesh(_vfs, s).Cache()
        );

        private static readonly Resource<IMultiResolutionMesh> _multiResolutionMesh = new(
            s => new MultiResolutionMesh(_vfs, s).Cache()
        );

        private static readonly Resource<IMorphMesh> _morphMesh = new(
            s => new MorphMesh(_vfs, s).Cache()
        );

        private static readonly Resource<IFont> _font = new(
            s => new Font(_vfs, s).Cache()
        );

        private static readonly Resource<SoundData> _sound = new(
            s =>
            {
                var node = _vfs.Find(s);
                return node == null ? null : SoundCreator.ConvertWavByteArrayToFloatArray(node.Buffer.Bytes);
            }
        );

        private static readonly Resource<GameObject> _prefab = new(s =>
        {
            // Lookup is done in following places:
            // 1. CONTEXT_NAME/Prefabs/... - overwrites lookup path below, used for specific prefabs, for current context (HVR, Flat, ...)
            // 2. Prefabs/... - Located inside core module (GVR), if we don't need special handling.
            var contextPrefixPath = $"{GuzContext.InteractionAdapter.GetContextName()}/{s}";
            return new[] { contextPrefixPath, s }.Select(Resources.Load<GameObject>)
                .FirstOrDefault(newPrefab => newPrefab != null);
        });

        public static void Init(string root)
        {
            var workPath = FindWorkPath(root);
            var diskPaths = FindDiskPaths(root);

            diskPaths.ForEach(v => _vfs.MountDisk(v, VfsOverwriteBehavior.Older));
            _vfs.Mount(Path.GetFullPath(workPath), "/_work", VfsOverwriteBehavior.All);

            _dmLoader.AddResolver(name =>
            {
                var node = _vfs.Find(name);
                return node?.Buffer.Bytes;
            });
        }

        [CanBeNull]
        public static ITexture TryGetTexture([NotNull] string key)
        {
            return _textures.TryLoad($"{GetPreparedKey(key)}-c.tex", out var item) ? item : null;
        }

        [CanBeNull]
        public static IModelScript TryGetModelScript([NotNull] string key)
        {
            return _modelScript.TryLoad($"{GetPreparedKey(key)}.mds", out var item) ? item : null;
        }

        [CanBeNull]
        public static IModelAnimation TryGetModelAnimation([NotNull] string mds, [NotNull] string key)
        {
            key = $"{GetPreparedKey(mds)}-{GetPreparedKey(key)}.man";
            return _modelAnimation.TryLoad(key, out var item) ? item : null;
        }

        [CanBeNull]
        public static IMesh TryGetMesh([NotNull] string key)
        {
            return _mesh.TryLoad($"{GetPreparedKey(key)}.msh", out var item) ? item : null;
        }

        [CanBeNull]
        public static IModelHierarchy TryGetModelHierarchy([NotNull] string key)
        {
            return _modelHierarchy.TryLoad($"{GetPreparedKey(key)}.mdh", out var item) ? item : null;
        }

        [CanBeNull]
        public static IModel TryGetModel([NotNull] string key)
        {
            return _model.TryLoad($"{GetPreparedKey(key)}.mdl", out var item) ? item : null;
        }

        [CanBeNull]
        public static IModelMesh TryGetModelMesh([NotNull] string key)
        {
            return _modelMesh.TryLoad($"{GetPreparedKey(key)}.mdm", out var item) ? item : null;
        }

        [CanBeNull]
        public static IMultiResolutionMesh TryGetMultiResolutionMesh([NotNull] string key)
        {
            return _multiResolutionMesh.TryLoad($"{GetPreparedKey(key)}.mrm", out var item) ? item : null;
        }

        [CanBeNull]
        public static IMorphMesh TryGetMorphMesh([NotNull] string key)
        {
            return _morphMesh.TryLoad($"{GetPreparedKey(key)}.mmb", out var item) ? item : null;
        }

        [CanBeNull]
        public static IFont TryGetFont([NotNull] string key)
        {
            return _font.TryLoad($"{GetPreparedKey(key)}.fnt", out var item) ? item : null;
        }

        [CanBeNull]
        public static SoundData TryGetSound([NotNull] string key)
        {
            return _sound.TryLoad($"{GetPreparedKey(key)}.wav", out var item) ? item : null;
        }

        [CanBeNull]
        public static Segment TryGetSegment([NotNull] string key)
        {
            // NOTE(lmichaelis): There is no caching required here, since the loader
            //                   already caches segments upon loading them
            return _dmLoader.GetSegment(key);
        }

        /// <summary>
        /// Load a <b>new</b> <see cref="DaedalusVm"/> from the assets by name. VMs are not cached, thus this call will
        /// <b>always create a new VM</b>. You most likely need to use the prepared VM instances in
        /// <see cref="GameData"/> instead.
        /// </summary>
        [CanBeNull]
        public static DaedalusVm TryGetDaedalusVm([NotNull] string key)
        {
            // NOTE(lmichaelis): These are not cached, since they contain internal state
            //                   which should not be shared.
            return new DaedalusVm(_vfs, $"{GetPreparedKey(key)}.dat");
        }

        [CanBeNull]
        public static ZenKit.World TryGetWorld([NotNull] string key)
        {
            return new ZenKit.World(_vfs, $"{GetPreparedKey(key)}.zen");
        }

        [CanBeNull]
        public static ZenKit.World TryGetWorld([NotNull] string key, GameVersion version)
        {
            return new ZenKit.World(_vfs, $"{GetPreparedKey(key)}.zen", version);
        }

        [CanBeNull]
        private static GameObject TryGetPrefab(PrefabType key)
        {
            return _prefab.TryLoad(key.Path(), out var item) ? item : null;
        }

        [CanBeNull]
        public static GameObject TryGetPrefabObject(PrefabType key)
        {
            return Object.Instantiate(TryGetPrefab(key));
        }

        [NotNull]
        private static string GetPreparedKey([NotNull] string key)
        {
            var extension = Path.GetExtension(key);
            var withoutExtension = extension == string.Empty ? key : key.Replace(extension, "");
            return withoutExtension.ToLower();
        }


        /// <summary>
        /// Determines the absolute path to a Gothic installation's <c>_work</c> directory given the installation's
        /// root directory. It does the lookup case-insensitively.
        /// </summary>
        /// <param name="root">A path to a Gothic installation's root directory</param>
        /// <returns>The absolute path to the <c>_work</c> folder of the Gothic installation</returns>
        private static string FindWorkPath(string root)
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
        private static List<string> FindDiskPaths(string root)
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
