using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Util;
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
        private string _fileEndingTextures = "textures.json.gz";
        private string _fileEndingWorld = "world.json.gz";
        private string _fileEndingVobs = "vobs.json.gz";

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

            /// If a GameObject contains a mesh, we store this information here.
            public MeshCacheEntry MeshData;
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


        public void Init()
        {
            _cacheRootFolderPath = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/";
        }

        /// <summary>
        /// We check for all required cache files once. If any of these are missing, the whole cache is marked as invalid.
        /// </summary>
        public bool DoCacheFilesExist(string worldName)
        {
            return File.Exists(BuildFilePathName(worldName, _fileEndingTextures)) &&
                   File.Exists(BuildFilePathName(worldName, _fileEndingWorld)) &&
                   File.Exists(BuildFilePathName(worldName, _fileEndingVobs));
        }

        public async Task SaveCache(GameObject worldRootGo, GameObject vobsRootGo, string fileName)
        {
            try
            {
                Directory.CreateDirectory(_cacheRootFolderPath);

                var textureData = CollectTextureData();
                var worldData = CollectWorldData(worldRootGo.transform);
                var vobsData = CollectVobsData(vobsRootGo.transform);

                await SaveCacheFile(textureData, worldData, vobsData, fileName);

                // As we stored the Meshes and TextureArrays, we can safely remove all the data from Managers now.
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
                var textureString = await ReadCompressedData(BuildFilePathName(worldName, _fileEndingTextures));
                var worldString = await ReadCompressedData(BuildFilePathName(worldName, _fileEndingWorld));
                var vobsString = await ReadCompressedData(BuildFilePathName(worldName, _fileEndingVobs));

                var textureJson = await ParseJson<TextureArrayContainer>(textureString);
                var worldJson = await ParseJson<CacheContainer>(worldString);
                var vobsJson = await ParseJson<CacheContainer>(vobsString);

                await GameGlobals.TextureArray.BuildTextureArraysFromCache(textureJson);
                await CreateFromCache(rootGo, worldJson.Root);
                await CreateFromCache(rootGo, vobsJson.Root);

                // As we created the Meshes and TextureArrays, we can safely remove all the data from Managers now.
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
                MeshData = GetWorldMeshData(currentElement.gameObject)
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
                MeshData = GetVobsMeshData(currentElement.gameObject)
            };

            for (var i = 0; i < currentElement.childCount; i++)
            {
                entry.Children.Add(WalkVob(currentElement.GetChild(i)));
            }

            return entry;
        }

        private MeshCacheEntry GetWorldMeshData(GameObject currentElement)
        {
            if (!currentElement.TryGetComponent<MeshFilter>(out var meshFilter) || !currentElement.TryGetComponent<Renderer>(out var renderer))
            {
                return null;
            }

            var textureArrayElement = GameGlobals.TextureArray.WorldMeshRenderersForTextureArray
                .FirstOrDefault(i => i.Renderer == renderer);

            if (textureArrayElement == default)
            {
                Debug.LogError("No TextureArray element for this renderer found. Skipping entry...");
                return null;
            }

            var mesh = meshFilter.sharedMesh;
            var uvs = new List<Vector4>();
            mesh.GetUVs(0, uvs);

            var uv1 = new List<Vector2>();
            mesh.GetUVs(1, uv1);

            var data = new MeshCacheEntry()
            {
                TextureTypes = new[] {textureArrayElement.SubmeshData.TextureArrayType}, // We have only one single entry per world mesh chunk.
                MaterialGroup = textureArrayElement.SubmeshData.Material.Group,
                Vertices = mesh.vertices,
                SubMeshTriangleCounts = new int[]{mesh.triangles.Length}, // There's only one SubMesh per world chunk. No submeshing needed and therefore we always fetch whole length.
                UV0 = uvs.ToArray(),
                UV1 = uv1.ToArray(),
                Colors = mesh.colors32 // TODO - Do we use colors or colors32 for World and/or VOBs?
            };

            return data;
        }

        private MeshCacheEntry GetVobsMeshData(GameObject currentElement)
        {
            if (!currentElement.TryGetComponent<MeshFilter>(out var meshFilter) || !currentElement.TryGetComponent<Renderer>(out var renderer))
            {
                return null;
            }

            var mesh = meshFilter.sharedMesh;
            var textureArrayElement = GameGlobals.TextureArray.VobMeshesForTextureArray[mesh];

            if (textureArrayElement == null)
            {
                Debug.LogError("No TextureArray element for this renderer found. Skipping entry...");
                return null;
            }

            var uv0 = new List<Vector4>();
            mesh.GetUVs(0, uv0);

            var triangleCounts = new int[mesh.subMeshCount];
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                // We never ever reuse triangles. We can therefore simply store the length of array and later recreate it with Range(0,n).
                triangleCounts[i] = mesh.GetTriangles(i).Length;
            }

            var data = new MeshCacheEntry()
            {
                TextureTypes = textureArrayElement.TextureTypes.ToArray(),
                MaterialGroup = MaterialGroup.Undefined, // Not needed for VOBs
                Vertices = mesh.vertices,
                SubMeshTriangleCounts = triangleCounts,
                UV0 = uv0.ToArray(),
                Colors = mesh.colors32, // TODO - Do we use colors or colors32 for World and/or VOBs?
            };

            return data;
        }

        private async Task CreateFromCache(GameObject parentGo, CacheEntry entry)
        {
            var go = new GameObject(entry.Name);
            go.transform.SetParent(parentGo.transform);
            go.transform.SetLocalPositionAndRotation(entry.LocalPosition, entry.LocalRotation);

            var meshData = entry.MeshData;

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

                var meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var meshRenderer = go.AddComponent<MeshRenderer>();
                GameGlobals.TextureArray.AssignTextureArray(entry, meshRenderer);
            }

            await FrameSkipper.TrySkipToNextFrame();

            foreach (var child in entry.Children)
            {
                await CreateFromCache(go, child);
            }
        }

        private async Task SaveCacheFile(TextureArrayContainer textureData, CacheContainer worldData, CacheContainer vobsData, string fileName)
        {

            string textureJson = null;
            string worldJson = null;
            string vobsJson = null;

            // We need to call loading the data in a separate thread to unblock main thread (VR movement etc.) during this IO heavy operation.
            await Task.Run(() =>
            {
                textureJson = JsonUtility.ToJson(textureData);
                worldJson = JsonUtility.ToJson(worldData);
                vobsJson = JsonUtility.ToJson(vobsData);
            });

            await WriteCompressedData(textureJson, BuildFilePathName(fileName, _fileEndingTextures));
            await WriteCompressedData(worldJson, BuildFilePathName(fileName, _fileEndingWorld));
            await WriteCompressedData(vobsJson, BuildFilePathName(fileName, _fileEndingVobs));
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


        private string BuildFilePathName(string fileName, string fileEnding)
        {
            return $"{_cacheRootFolderPath}/{fileName}.{fileEnding}";
        }
    }
}
