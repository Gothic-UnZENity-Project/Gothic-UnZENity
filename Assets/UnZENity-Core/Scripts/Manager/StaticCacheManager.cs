using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Util;
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
        private string _cacheRootFolderPath;
        private const string _fileNameMetadata = "metadata.json";
        private const string _fileNameMeshes = "meshes.json.gz";
        private const string _fileNameTextures = "textures.json.gz";
        private const string _fileNameWorld = "world.json.gz";
        private const string _fileNameVobs = "vobs.json.gz";


        // Will be used for storing and retrieving world+static vobs.
        // It helps us to store a mesh for a VOB only once as objects will reference its index when a VOB is created multiple times.
        private List<(Mesh Mesh, MeshCacheEntry CacheEntry)> _tempMeshCacheEntries = new();

        [Serializable]
        public class MetadataContainer
        {
            public string Version;
            public string CreationTime;
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
            public TextureArrayManager.TextureArrayTypes[] TextureTypes;
            public int[] SubMeshTriangleCounts;
            public MaterialGroup MaterialGroup;
            public Vector3[] Vertices;
            public Vector4[] UV0;
            public Vector2[] UV1;
            public Color32[] Colors;
        }

        /// <summary>
        /// Store information about TextureArrays which will be used to recreate texture array once loaded.
        /// HINT: JsonUtility doesn't support Dictionaries. Therefore, using subclasses in lists.
        /// </summary>
        [Serializable]
        public class TextureArrayContainer
        {
            public List<TextureTypeEntry> TextureTypeEntries = new();
        }

        [Serializable]
        public class TextureTypeEntry
        {
            public TextureArrayManager.TextureArrayTypes TextureType;

            /// <summary>
            /// Every time a texture would be needed for a mesh the first time, its entry is added here.
            /// UV values of meshes already contain this information (e.g. v4(0,0,2,0) -> 2 would be marking index 3 of these entries below)
            /// </summary>
            public List<TextureArrayManager.TextureData> Textures = new();
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

            /// If a GameObject contains a mesh, we fetch the corresponding mesh from cache and store its ID here.
            public int MeshCacheEntryId = -1;
        }


        public void Init()
        {
            _cacheRootFolderPath = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/";
        }

        /// <summary>
        /// We check for all required cache files once. If any of these are missing, the whole cache is marked as invalid.
        /// </summary>
        public bool DoCacheFilesExist(string worldName)
        {
            return File.Exists(BuildFilePathName(worldName, _fileNameMetadata)) &&
                   File.Exists(BuildFilePathName(worldName, _fileNameMeshes)) &&
                   File.Exists(BuildFilePathName(worldName, _fileNameTextures)) &&
                   File.Exists(BuildFilePathName(worldName, _fileNameWorld)) &&
                   File.Exists(BuildFilePathName(worldName, _fileNameVobs));
        }

        public MetadataContainer ReadMetadata(string worldName)
        {
            var metadataString = File.ReadAllText(BuildFilePathName(worldName, _fileNameMetadata));
            return JsonUtility.FromJson<MetadataContainer>(metadataString);
        }

        public async Task SaveCache(GameObject worldRootGo, GameObject vobsRootGo, string worldName)
        {
            try
            {
                // Create cache folder if it doesn't exist
                Directory.CreateDirectory(BuildFilePathName(worldName, ""));
                // Cleanup existing files (if we renamed some with a new version, these stalled files will be deleted as well)
                Directory.EnumerateFiles(BuildFilePathName(worldName, "")).ForEach(File.Delete);

                var metadata = new MetadataContainer()
                {
                    Version = Constants.StaticCacheVersion,
                    CreationTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.K")
                };
                var textureData = CollectTextureData();
                var worldData = CollectWorldData(worldRootGo.transform);
                var vobsData = CollectVobsData(vobsRootGo.transform);
                var meshData = new MeshContainer()
                {
                    Meshes = _tempMeshCacheEntries.Select(i => i.CacheEntry).ToList()
                };

                await SaveCacheFile(metadata, meshData, textureData, worldData, vobsData, worldName);

                // As we stored the Meshes and TextureArrays, we can safely remove all the data from Managers now.
                _tempMeshCacheEntries.ClearAndReleaseMemory();
                GameGlobals.TextureArray.Dispose();
                TextureCache.Dispose();
                MultiTypeCache.Dispose();
                MorphMeshCache.Dispose(); // We _accidentally_ create morph caches (as we didn't update the AbstractMeshBuilder logic)
            }
            catch (Exception e)
            {
                Debug.LogError($"There was some error while storing Mesh Cache: {e}");
                throw;
            }
        }

        public async Task LoadCache(GameObject rootGo, string worldName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var meshString = await ReadCompressedData(BuildFilePathName(worldName, _fileNameMeshes));
                var textureString = await ReadCompressedData(BuildFilePathName(worldName, _fileNameTextures));
                var worldString = await ReadCompressedData(BuildFilePathName(worldName, _fileNameWorld));
                var vobsString = await ReadCompressedData(BuildFilePathName(worldName, _fileNameVobs));

                var meshJson = await ParseJson<MeshContainer>(meshString);
                var textureJson = await ParseJson<TextureArrayContainer>(textureString);
                var worldJson = await ParseJson<CacheContainer>(worldString);
                var vobsJson = await ParseJson<CacheContainer>(vobsString);

                await GameGlobals.TextureArray.BuildTextureArraysFromCache(textureJson);
                await RestoreMeshesFromCache(meshJson);
                await CreateFromCache(rootGo, worldJson.Root);
                await CreateFromCache(rootGo, vobsJson.Root);

                // As we created the Meshes and TextureArrays, we can safely remove all the data from Managers now.
                _tempMeshCacheEntries.ClearAndReleaseMemory();
                GameGlobals.TextureArray.Dispose();
                MultiTypeCache.Dispose();
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


        private TextureArrayContainer CollectTextureData()
        {
            var dataToSave = new TextureArrayContainer();

            // Set texture information.
            // i.e. Which texture type (and ultimately which world chunk based on texture type) has the following textures.?
            foreach (var data in GameGlobals.TextureArray.TexturesToIncludeInArray)
            {
                var entry = new TextureTypeEntry()
                {
                    TextureType = data.Key,
                    Textures = data.Value.Select(t => t.TextureData).ToList()
                };

                dataToSave.TextureTypeEntries.Add(entry);
            }

            return  dataToSave;
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
                LocalRotation = currentElement.localRotation,
                MeshCacheEntryId = TryGetWorldMeshDataId(currentElement.gameObject)
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
                LocalRotation = currentElement.localRotation,
                MeshCacheEntryId = TryGetVobMeshDataId(currentElement.gameObject)
            };

            for (var i = 0; i < currentElement.childCount; i++)
            {
                entry.Children.Add(WalkVob(currentElement.GetChild(i)));
            }

            return entry;
        }

        /// <summary>
        /// We store Meshes on a separate List. We therefore need to fetch the corresponding ID for our mesh only.
        /// Yes, World chunk meshes aren't reused, but leveraging the same logic as with VOBs helps us to simplify loading logic.
        /// </summary>
        private int TryGetWorldMeshDataId(GameObject currentElement)
        {
            if (!currentElement.TryGetComponent<MeshFilter>(out var meshFilter) || !currentElement.TryGetComponent<Renderer>(out var renderer))
            {
                return -1;
            }

            var textureArrayElement = GameGlobals.TextureArray.WorldMeshRenderersForTextureArray
                .FirstOrDefault(i => i.Renderer == renderer);

            if (textureArrayElement == default)
            {
                Debug.LogError("No TextureArray element for this renderer found. Skipping entry...");
                return -1;
            }

            var mesh = meshFilter.sharedMesh;

            if (GetOrCreateMeshCacheEntry(mesh, out var meshCacheEntry))
            {
                // Set specific data for World mesh chunks
                meshCacheEntry.TextureTypes = new[] { textureArrayElement.SubmeshData.TextureArrayType };
                meshCacheEntry.MaterialGroup = textureArrayElement.SubmeshData.Material.Group;
            }

            return _tempMeshCacheEntries.IndexOf((mesh, meshCacheEntry));
        }

        /// <summary>
        /// We store Meshes on a separate List. We therefore need to fetch the corresponding ID for our mesh only.
        /// </summary>
        private int TryGetVobMeshDataId(GameObject currentElement)
        {
            if (!currentElement.TryGetComponent<MeshFilter>(out var meshFilter) || !currentElement.TryGetComponent<Renderer>(out var renderer))
            {
                return -1;
            }

            var mesh = meshFilter.sharedMesh;
            var textureArrayElement = GameGlobals.TextureArray.VobMeshesForTextureArray[mesh];

            if (textureArrayElement == null)
            {
                Debug.LogError("No TextureArray element for this renderer found. Skipping entry...");
                return -1;
            }

            if (GetOrCreateMeshCacheEntry(mesh, out var meshCacheEntry))
            {
                meshCacheEntry.TextureTypes = textureArrayElement.TextureTypes.ToArray();
                meshCacheEntry.MaterialGroup = MaterialGroup.Undefined; // Not needed for VOBs
            }

            return _tempMeshCacheEntries.IndexOf((mesh, meshCacheEntry));
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

        private async Task RestoreMeshesFromCache(MeshContainer meshContainer)
        {
            foreach (var meshData in meshContainer.Meshes)
            {
                // Unity's JsonSerialize will (mostly) always store empty classes with default values. We therefore check if MeshData has no triangles (aka is NULL).
                if (meshData != null && meshData.Vertices != null)
                {
                    var mesh = new Mesh();
                    mesh.vertices = meshData.Vertices;
                    mesh.SetUVs(0, meshData.UV0);
                    mesh.colors32 = meshData.Colors;

                    // We leverage this one for water world chunks.
                    if (meshData.UV1 != null && meshData.UV1.Any())
                    {
                        mesh.SetUVs(1, meshData.UV1);
                    }

                    mesh.subMeshCount = meshData.SubMeshTriangleCounts.Length;
                    var triangleStartOffset = 0;
                    for (var i = 0; i < mesh.subMeshCount; i++)
                    {
                        var currentTriangleCount = meshData.SubMeshTriangleCounts[i];
                        // Create entries like: [0, 1, 2, ..., n-1)
                        var currentTriangles = Enumerable.Range(triangleStartOffset, currentTriangleCount).ToArray();
                        mesh.SetTriangles(currentTriangles, i);

                        // Triangles are never reused. i.e. when submeshing, we have first part triangles for submesh0, then submesh1, ...
                        // As triangles aren't reused, we need to count up. e.g. submesh1 might start with triangleIndex=10, but not 0!
                        triangleStartOffset += currentTriangleCount;
                    }

                    _tempMeshCacheEntries.Add((mesh, meshData));

                    await FrameSkipper.TrySkipToNextFrame();
                }
            }
        }

        private async Task CreateFromCache(GameObject parentGo, CacheEntry entry)
        {
            var go = new GameObject(entry.Name);
            go.transform.SetParent(parentGo.transform);
            go.transform.SetLocalPositionAndRotation(entry.LocalPosition, entry.LocalRotation);

            if (entry.MeshCacheEntryId >= 0)
            {
                var meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = _tempMeshCacheEntries[entry.MeshCacheEntryId].Mesh;

                var meshRenderer = go.AddComponent<MeshRenderer>();
                GameGlobals.TextureArray.AssignTextureArray(_tempMeshCacheEntries[entry.MeshCacheEntryId].CacheEntry, meshRenderer);
            }

            await FrameSkipper.TrySkipToNextFrame();

            foreach (var child in entry.Children)
            {
                await CreateFromCache(go, child);
            }
        }

        private async Task SaveCacheFile(MetadataContainer metadata, MeshContainer meshData, TextureArrayContainer textureData, CacheContainer worldData, CacheContainer vobsData, string fileName)
        {
            string metadataJson = null;
            string meshJson = null;
            string textureJson = null;
            string worldJson = null;
            string vobsJson = null;

            // We need to call loading the data in a separate thread to unblock main thread (VR movement etc.) during this IO heavy operation.
            await Task.Run(() =>
            {
                metadataJson = JsonUtility.ToJson(metadata, true);
                meshJson = JsonUtility.ToJson(meshData);
                textureJson = JsonUtility.ToJson(textureData);
                worldJson = JsonUtility.ToJson(worldData);
                vobsJson = JsonUtility.ToJson(vobsData);
            });

            await WriteData(metadataJson, BuildFilePathName(fileName, _fileNameMetadata));
            await WriteCompressedData(meshJson, BuildFilePathName(fileName, _fileNameMeshes));
            await WriteCompressedData(textureJson, BuildFilePathName(fileName, _fileNameTextures));
            await WriteCompressedData(worldJson, BuildFilePathName(fileName, _fileNameWorld));
            await WriteCompressedData(vobsJson, BuildFilePathName(fileName, _fileNameVobs));
        }

        private async Task WriteData(string data, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var writer = new StreamWriter(fileStream))
            {
                await writer.WriteAsync(data);
            }
        }

        private async Task WriteCompressedData(string data, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzipStream))
            {
                await writer.WriteAsync(data);
            }
        }

        private async Task<string> ReadCompressedData(string filePath)
        {
            string data = null;

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                // We need to call loading the data in a separate thread to unblock main thread (VR movement etc.) during this IO heavy operation.
                await Task.Run(() =>
                {
                    data = reader.ReadToEnd();
                });

                return data;
            }
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


        private string BuildFilePathName(string worldName, string fileName)
        {
            return $"{_cacheRootFolderPath}/{worldName}/{fileName}";
        }
    }
}
