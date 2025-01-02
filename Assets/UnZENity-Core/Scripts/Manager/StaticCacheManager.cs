using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Caches.StaticCache;
using GUZ.Core.Config;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager
{
    public class StaticCacheManager
    {
        private const string _gzipExt = ".gz";

        private const string _fileNameGlobalMetadata = "metadata.json";
        private const string _fileNameGlobalVobBounds = "vob-bounds.json";
        private const string _fileNameGlobalTextureArrayData = "texture-arrays.json";

        private const string _fileNameWorldChunks = "world-chunks.json";

        private string _cacheRootFolderPath;
        private bool _configIsCompressed;


        private bool _isGlobalCacheLoaded;

        public Dictionary<string, Bounds> LoadedVobsBounds { get; private set; }

        public Dictionary<string, int> LoadedTextureInfoOpaque { get; private set; }
        public Dictionary<string, int> LoadedTextureInfoTransparent { get; private set; }
        public Dictionary<string, int> LoadedTextureInfoWater { get; private set; }

        public WorldChunkContainer LoadedWorldChunks;


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
        public class WorldChunkContainer
        {
            public List<WorldChunkCacheCreator.WorldChunk> OpaqueChunks;
            public List<WorldChunkCacheCreator.WorldChunk> TransparentChunks;
            public List<WorldChunkCacheCreator.WorldChunk> WaterChunks;
        }


        public void Init(DeveloperConfig config)
        {
            _configIsCompressed = config.CompressStaticCacheFiles;
            _cacheRootFolderPath = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/";
        }

        /// <summary>
        /// We check for all required cache files once. If any of these are missing, the whole cache is marked as invalid.
        /// </summary>
        public bool DoCacheFilesExist(string[] worldNames)
        {
            // Check all world specific files.
            foreach (var worldName in worldNames)
            {
                if (!File.Exists(BuildFilePathName(_fileNameWorldChunks, worldName)))
                {
                    return false;
                }
            }

            // Global files
            return File.Exists(BuildFilePathName(_fileNameGlobalMetadata)) &&
                   File.Exists(BuildFilePathName(_fileNameGlobalTextureArrayData)) &&
                   File.Exists(BuildFilePathName(_fileNameGlobalVobBounds));
        }

        /// <summary>
        /// Inside PreCachingScene, we want to fetch this file separately. Its value will tell us if we can skip caching this time.
        /// </summary>
        public async Task<MetadataContainer> ReadMetadata()
        {
            var metadataString = await ReadData(BuildFilePathName(_fileNameGlobalMetadata));
            return await ParseJson<MetadataContainer>(metadataString);
        }

        /// <summary>
        /// Create cache folder and delete existing - stalled - files (if any).
        /// </summary>
        public void InitCacheFolder()
        {
            var folderToCleanUp = BuildFilePathName("");

            // Create cache folder if it doesn't exist
            Directory.CreateDirectory(folderToCleanUp);

            // Cleanup existing files and directories (if we renamed some with a new version, these stalled files will be deleted as well)
            Directory.EnumerateFiles(folderToCleanUp).ForEach(File.Delete);
            Directory.EnumerateDirectories(folderToCleanUp).ForEach(dir => Directory.Delete(dir, true));
        }

        public async Task SaveGlobalCache(Dictionary<string, Bounds> vobBounds,
            Dictionary<string, (int maxDimension, TextureCache.TextureArrayTypes textureType)> textureArrayInformation)
        {
            try
            {
                var metadataContainer = new MetadataContainer
                {
                    Version = Constants.StaticCacheVersion,
                    CreationTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.K")
                };
                await SaveCacheFile(metadataContainer, BuildFilePathName(_fileNameGlobalMetadata));

                var vobBoundsContainer = new VobBoundsContainer
                {
                    BoundsEntries = vobBounds.Select(i => new VobBoundsEntry(i.Key, i.Value)).ToList()
                };
                await SaveCacheFile(vobBoundsContainer, BuildFilePathName(_fileNameGlobalVobBounds));

                var textureArrayContainer = new TextureArrayContainer
                {
                    TexturesOpaque = textureArrayInformation.Where(i => i.Value.textureType == TextureCache.TextureArrayTypes.Opaque).Select(i => new TextureArrayEntry(i.Key, i.Value.maxDimension)).ToList(),
                    TexturesTransparent = textureArrayInformation.Where(i => i.Value.textureType == TextureCache.TextureArrayTypes.Transparent).Select(i => new TextureArrayEntry(i.Key, i.Value.maxDimension)).ToList(),
                    TexturesWater = textureArrayInformation.Where(i => i.Value.textureType == TextureCache.TextureArrayTypes.Water).Select(i => new TextureArrayEntry(i.Key, i.Value.maxDimension)).ToList(),
                };
                await SaveCacheFile(textureArrayContainer, BuildFilePathName(_fileNameGlobalTextureArrayData));
            }
            catch (Exception e)
            {
                Debug.LogError($"There was some error while storing Global Cache: {e}");
                throw;
            }
        }

        public async Task SaveWorldCache(
            string worldName,
            Dictionary<TextureCache.TextureArrayTypes, List<WorldChunkCacheCreator.WorldChunk>> mergedChunksByLights)
        {
            try
            {
                var worldChunkData = new WorldChunkContainer()
                {
                    OpaqueChunks = mergedChunksByLights[TextureCache.TextureArrayTypes.Opaque],
                    TransparentChunks = mergedChunksByLights[TextureCache.TextureArrayTypes.Transparent],
                    WaterChunks = mergedChunksByLights[TextureCache.TextureArrayTypes.Water],
                };

                await SaveCacheFile(worldChunkData, BuildFilePathName(_fileNameWorldChunks, worldName));
            }
            catch (Exception e)
            {
                Debug.LogError($"There was some error while storing Mesh Cache: {e}");
                throw;
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
                var vobBoundsString = await ReadData(BuildFilePathName(_fileNameGlobalVobBounds));
                var textureArrayString = await ReadData(BuildFilePathName(_fileNameGlobalTextureArrayData));

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

        public async Task LoadWorldCache(string worldName)
        {
            var stopwatch = Stopwatch.StartNew();

            var worldChunkString = await ReadData(BuildFilePathName(_fileNameWorldChunks, worldName));
            LoadedWorldChunks = await ParseJson<WorldChunkContainer>(worldChunkString);

            stopwatch.Log("Loading global cache done.");
        }

        private async Task SaveCacheFile(object data, string filePathName)
        {
            string json = null;
            var prettyPrint = !_configIsCompressed;

            // We need to call loading the data in a separate thread to unblock main thread (VR movement etc.) during this IO heavy operation.
            await Task.Run(() =>
            {
                json = JsonUtility.ToJson(data, prettyPrint);
            });

            await WriteData(json, filePathName);
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

        private string BuildFilePathName(string fileName, string worldName = null)
        {
            var suffix = _configIsCompressed ? _gzipExt : "";

            if (worldName == null)
            {
                return $"{_cacheRootFolderPath}/{fileName}{suffix}";
            }
            else
            {
                return $"{_cacheRootFolderPath}/{worldName}-{fileName}{suffix}";
            }
        }
    }
}
