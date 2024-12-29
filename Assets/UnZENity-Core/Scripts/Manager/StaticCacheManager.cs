using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using UnityEngine;
using ZenKit;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Debug = UnityEngine.Debug;
using Mesh = UnityEngine.Mesh;

namespace GUZ.Core.Manager
{
    public class StaticCacheManager
    {
        private const string _gzipExt = ".gz";
        private const string _fileNameMetadata = "metadata.json";
        private const string _fileNameVobBounds = "vob-bounds.json";
        private const string _fileNameTextureArrayData = "texture-arrays.json";
        private const string _fileNameMeshes = "meshes.json";
        private const string _fileNameVobs = "vobs.json";

        private string _cacheRootFolderPath;

        private bool _configIsCompressed;


        // Will be used for storing and retrieving world+static vobs.
        // It helps us to store a mesh for a VOB only once as objects will reference its index when a VOB is created multiple times.
        private List<(Mesh Mesh, MeshCacheEntry CacheEntry)> _tempMeshCacheEntries = new();
        private bool _isGlobalCacheLoaded;

        public Dictionary<string, Bounds> LoadedVobsBounds { get; private set; }

        public Dictionary<string, int> LoadedTextureInfoOpaque { get; private set; }
        public Dictionary<string, int> LoadedTextureInfoTransparent { get; private set; }
        public Dictionary<string, int> LoadedTextureInfoWater { get; private set; }

        [Serializable]
        public class MetadataContainer
        {
            public string Version;
            public string CreationTime;
        }

        [Serializable]
        public class VobBoundsContainer
        {
            public List<VobBoundsEntry> BoundsEntries;
        }

        [Serializable]
        public class VobBoundsEntry
        {
            public VobBoundsEntry(string meshName, Bounds bounds)
            {
                MeshName = meshName;
                Bounds = bounds;
            }

            public string MeshName;
            public Bounds Bounds;
        }

        [Serializable]
        public class TextureArrayContainer
        {
            public List<TextureArrayEntry> TexturesOpaque;
            public List<TextureArrayEntry> TexturesTransparent;
            public List<TextureArrayEntry> TexturesWater;
        }

        [Serializable]
        public class TextureArrayEntry
        {
            public TextureArrayEntry(string textureName, int maxDimension)
            {
                TextureName = textureName;
                MaxDimension = maxDimension;
            }

            public string TextureName;
            public int MaxDimension;
        }

        [Serializable]
        public class MeshContainer
        {
            public List<MeshCacheEntry> Meshes;
        }

        /// <summary>
        /// Mesh data of a GameObject
        /// </summary>
        [Serializable]
        public class MeshCacheEntry
        {
            public int[] SubMeshTriangleCounts;
            public MaterialGroup MaterialGroup;
            public Vector3[] Vertices;
            public Vector4[] UV0;
            public Vector2[] UV1;
            public Color32[] Colors;
        }

        [Serializable]
        public class CacheContainer
        {
            public CacheEntry Root;
        }

        /// <summary>
        /// An entry represents a GameObject in hierarchy
        /// </summary>
        [Serializable]
        public class CacheEntry
        {
            public string Name;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;

            public List<CacheEntry> Children = new();
        }

        public void Init(DeveloperConfig config)
        {
            _configIsCompressed = config.CompressStaticCacheFiles;
            _cacheRootFolderPath = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/";
        }

        /// <summary>
        /// We check for all required cache files once. If any of these are missing, the whole cache is marked as invalid.
        /// </summary>
        public bool DoCacheFilesExist(string worldName)
        {
            return File.Exists(BuildWorldFilePathName(worldName, _fileNameMetadata)) &&
                   File.Exists(BuildWorldFilePathName(worldName, _fileNameMeshes)) &&
                   File.Exists(BuildWorldFilePathName(worldName, _fileNameVobs));
        }

        public MetadataContainer ReadMetadata(string worldName)
        {
            var metadataString = File.ReadAllText(BuildWorldFilePathName(worldName, _fileNameMetadata));
            return JsonUtility.FromJson<MetadataContainer>(metadataString);
        }

        public async Task SaveWorldCache(GameObject vobsRootGo, string worldName)
        {
            try
            {
                PrepareWorldFolder(worldName);

                var metadata = new MetadataContainer()
                {
                    Version = Constants.StaticCacheVersion,
                    CreationTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.K")
                };
                var vobsData = CollectVobsData(vobsRootGo.transform);
                var meshData = new MeshContainer()
                {
                    Meshes = _tempMeshCacheEntries.Select(i => i.CacheEntry).ToList()
                };

                await SaveWorldCacheFile(metadata, meshData, vobsData, worldName);
            }
            catch (Exception e)
            {
                Debug.LogError($"There was some error while storing Mesh Cache: {e}");
                throw;
            }
        }

        private void PrepareWorldFolder(string worldName)
        {
            // Create cache folder if it doesn't exist
            Directory.CreateDirectory(BuildWorldFilePathName(worldName, ""));
            // Cleanup existing files (if we renamed some with a new version, these stalled files will be deleted as well)
            Directory.EnumerateFiles(BuildWorldFilePathName(worldName, "")).ForEach(File.Delete);
        }

        private void PrepareGlobalFolder()
        {
            // Create cache folder if it doesn't exist
            Directory.CreateDirectory(BuildGlobalFilePathName(""));
            // Cleanup existing files (if we renamed some with a new version, these stalled files will be deleted as well)
            Directory.EnumerateFiles(BuildGlobalFilePathName("")).ForEach(File.Delete);
        }

        public async Task SaveGlobalCache(Dictionary<string, Bounds> vobBounds,
            Dictionary<string, (int maxDimension, TextureCache.TextureArrayTypes textureType)> textureArrayInformation)
        {
            try
            {
                PrepareGlobalFolder();

                var vobBoundsContainer = new VobBoundsContainer
                {
                    BoundsEntries = vobBounds.Select(i => new VobBoundsEntry(i.Key, i.Value)).ToList()
                };

                var textureArrayContainer = new TextureArrayContainer
                {
                    TexturesOpaque = textureArrayInformation.Where(i => i.Value.textureType == TextureCache.TextureArrayTypes.Opaque).Select(i => new TextureArrayEntry(i.Key, i.Value.maxDimension)).ToList(),
                    TexturesTransparent = textureArrayInformation.Where(i => i.Value.textureType == TextureCache.TextureArrayTypes.Transparent).Select(i => new TextureArrayEntry(i.Key, i.Value.maxDimension)).ToList(),
                    TexturesWater = textureArrayInformation.Where(i => i.Value.textureType == TextureCache.TextureArrayTypes.Water).Select(i => new TextureArrayEntry(i.Key, i.Value.maxDimension)).ToList(),
                };

                await SaveGlobalCacheFile(vobBoundsContainer, textureArrayContainer);
            }
            catch (Exception e)
            {
                Debug.LogError($"There was some error while storing Global Cache: {e}");
                throw;
            }
        }

        public async Task LoadCache(GameObject rootGo, string worldName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var meshString = await ReadData(BuildWorldFilePathName(worldName, _fileNameMeshes));
                var vobsString = await ReadData(BuildWorldFilePathName(worldName, _fileNameVobs));

                var meshJson = await ParseJson<MeshContainer>(meshString);
                var vobsJson = await ParseJson<CacheContainer>(vobsString);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            finally
            {
                stopwatch.Log("Loading cache done.");
            }
        }

        public async Task LoadGlobalCache()
        {
            if (_isGlobalCacheLoaded)
            {
                return;
            }
            _isGlobalCacheLoaded = true;
            
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var vobBoundsString = await ReadData(BuildGlobalFilePathName(_fileNameVobBounds));
                var textureArrayString = await ReadData(BuildGlobalFilePathName(_fileNameTextureArrayData));

                var vobBoundsContainer = await ParseJson<VobBoundsContainer>(vobBoundsString);
                var textureArrayContainer = await ParseJson<TextureArrayContainer>(textureArrayString);

                LoadedVobsBounds = vobBoundsContainer.BoundsEntries.ToDictionary(i => i.MeshName, i => i.Bounds);


                LoadedTextureInfoOpaque = textureArrayContainer.TexturesOpaque.ToDictionary(i => i.TextureName, i => i.MaxDimension);
                LoadedTextureInfoTransparent = textureArrayContainer.TexturesTransparent.ToDictionary(i => i.TextureName, i => i.MaxDimension);
                LoadedTextureInfoWater = textureArrayContainer.TexturesWater.ToDictionary(i => i.TextureName, i => i.MaxDimension);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            finally
            {
                stopwatch.Log("Loading global cache done.");
            }
        }

        private CacheContainer CollectWorldData(Transform worldRoot)
        {
            var data = new CacheContainer();

            data.Root = WalkWorld(worldRoot);

            return data;
        }

        private CacheContainer CollectVobsData(Transform vobsRoot)
        {
            var data = new CacheContainer();

            data.Root = WalkVob(vobsRoot);

            return data;
        }

        private CacheEntry WalkWorld(Transform currentElement)
        {
            var entry = new CacheEntry()
            {
                Name = currentElement.name,
                LocalPosition = currentElement.localPosition,
                LocalRotation = currentElement.localRotation
            };

            for (var i = 0; i < currentElement.childCount; i++)
            {
                entry.Children.Add(WalkWorld(currentElement.GetChild(i)));
            }

            return entry;
        }

        private CacheEntry WalkVob(Transform currentElement)
        {
            var entry = new CacheEntry()
            {
                Name = currentElement.name,
                LocalPosition = currentElement.localPosition,
                LocalRotation = currentElement.localRotation
            };

            for (var i = 0; i < currentElement.childCount; i++)
            {
                entry.Children.Add(WalkVob(currentElement.GetChild(i)));
            }

            return entry;
        }

        /// <summary>
        /// Return meshCacheEntry.
        /// If it's a new one, we return _true_.
        ///
        /// IMPORTANT: We assume, that meshes are already referenced inside same VOBs.
        ///   If they're all created new, we can't do our lookup as the objectIDs would differ.
        /// </summary>
        private bool /*isNew*/ GetOrCreateMeshCacheEntry(Mesh mesh, out MeshCacheEntry meshCacheEntry)
        {
            var foundItem = _tempMeshCacheEntries.FirstOrDefault(i => i.Mesh == mesh);

            if (foundItem != default)
            {
                meshCacheEntry = foundItem.CacheEntry;
                return false;
            }


            // Now let's create a new one.
            var uv0 = new List<Vector4>();
            mesh.GetUVs(0, uv0);

            var uv1 = new List<Vector2>();
            mesh.GetUVs(1, uv1);

            var triangleCounts = new int[mesh.subMeshCount];
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                // We never ever reuse triangles. We can therefore simply store the length of array and later recreate it with Range(0,n).
                triangleCounts[i] = mesh.GetTriangles(i).Length;
            }

            var newEntry = new MeshCacheEntry()
            {
                Vertices = mesh.vertices,
                SubMeshTriangleCounts = triangleCounts,
                UV0 = uv0.ToArray(),
                UV1 = uv1.ToArray(),
                Colors = mesh.colors32, // TODO - Do we use colors or colors32 for World and/or VOBs?
            };

            _tempMeshCacheEntries.Add((mesh, newEntry));

            meshCacheEntry = newEntry;
            return true;
        }

        private async Task SaveWorldCacheFile(MetadataContainer metadata, MeshContainer meshData, CacheContainer vobsData, string fileName)
        {
            string metadataJson = null;
            string meshJson = null;
            string vobsJson = null;

            // We need to call loading the data in a separate thread to unblock main thread (VR movement etc.) during this IO heavy operation.
            await Task.Run(() =>
            {
                metadataJson = JsonUtility.ToJson(metadata, true);
                meshJson = JsonUtility.ToJson(meshData);
                vobsJson = JsonUtility.ToJson(vobsData);
            });

            await WriteData(metadataJson, BuildWorldFilePathName(fileName, _fileNameMetadata));
            await WriteData(meshJson, BuildWorldFilePathName(fileName, _fileNameMeshes));
            await WriteData(vobsJson, BuildWorldFilePathName(fileName, _fileNameVobs));
        }

        private async Task SaveGlobalCacheFile(VobBoundsContainer vobBoundsContainer, TextureArrayContainer textureArrayContainer)
        {
            string vobBoundsJson = null;
            string vobTextureArrayJson = null;
            var prettyPrint = !_configIsCompressed;

            // We need to call loading the data in a separate thread to unblock main thread (VR movement etc.) during this IO heavy operation.
            await Task.Run(() =>
            {
                vobBoundsJson = JsonUtility.ToJson(vobBoundsContainer, prettyPrint);
                vobTextureArrayJson = JsonUtility.ToJson(textureArrayContainer, prettyPrint);
            });

            await WriteData(vobBoundsJson, BuildGlobalFilePathName(_fileNameVobBounds));
            await WriteData(vobTextureArrayJson, BuildGlobalFilePathName(_fileNameTextureArrayData));
        }

        private async Task WriteData(string data, string filePath)
        {
            if (_configIsCompressed)
            {
                filePath += _gzipExt;
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
                using (var writer = new StreamWriter(gzipStream))
                {
                    await writer.WriteAsync(data);
                }
            }
            else
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                using (var writer = new StreamWriter(fileStream))
                {
                    await writer.WriteAsync(data);
                }
            }
        }

        private async Task<string> ReadData(string filePath)
        {
            string data = null;

            if (_configIsCompressed)
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(gzipStream))
                {
                    // We need to call loading the data in a separate thread to unblock main thread (VR movement etc.) during this IO heavy operation.
                    await Task.Run(() =>
                    {
                        data = reader.ReadToEnd();
                    });
                }
            }
            else
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                using (var reader = new StreamReader(fileStream))
                {
                    // We need to call loading the data in a separate thread to unblock main thread (VR movement etc.) during this IO heavy operation.
                    await Task.Run(() =>
                    {
                        data = reader.ReadToEnd();
                    });
                }
            }

            return data;
        }

        /// <summary>
        /// Parse string to json dynamically.
        /// </summary>
        private async Task<T> ParseJson<T>(string json)
        {
            T data = default;

            // We need to call parsing the data in a separate thread to unblock main thread (VR movement etc.) during this CPU heavy operation.
            await Task.Run(() =>
            {
                data = JsonUtility.FromJson<T>(json);
            });

            return data;
        }

        private string BuildWorldFilePathName(string worldName, string fileName)
        {
            var suffix = _configIsCompressed ? _gzipExt : "";
            return $"{_cacheRootFolderPath}/{worldName}/{fileName}{suffix}";
        }

        private string BuildGlobalFilePathName(string fileName)
        {
            var suffix = _configIsCompressed ? _gzipExt : "";
            return $"{_cacheRootFolderPath}/Global/{fileName}{suffix}";
        }
    }
}
