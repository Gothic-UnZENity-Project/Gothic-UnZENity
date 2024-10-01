using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ZenKit;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Mesh = UnityEngine.Mesh;

namespace GUZ.Core.Manager
{
    public class StaticCacheManager
    {
        private string _cacheRootFolderPath;


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
            public MaterialGroup MaterialGroup;
            public int TriangleCount;
            public Vector3[] Vertices;
            public Vector4[] UVs;
            public Color32[] Colors;
        }


        public void Init()
        {
            _cacheRootFolderPath = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/";
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

            }
            catch (Exception e)
            {
                Debug.LogError($"There was some error while storing Mesh Cache: {e}");
                throw;
            }
        }

        public async Task LoadCache(GameObject rootGo, string fileName)
        {
            try
            {
                var textureJson = JsonUtility.FromJson<TextureArrayContainer>(ReadCompressedData(BuildFilePathName(fileName, "textureData.json.gz")));
                var worldJson = JsonUtility.FromJson<CacheContainer>(ReadCompressedData(BuildFilePathName(fileName, "worldMeshes.json.gz")));
                var vobsJson = JsonUtility.FromJson<CacheContainer>(ReadCompressedData(BuildFilePathName(fileName, "vobMeshes.json.gz")));

                await GameGlobals.TextureArray.BuildTextureArraysFromCache(textureJson);
                await CreateFromCache(rootGo, worldJson.Root);
                await CreateFromCache(rootGo, vobsJson.Root);
                GameGlobals.TextureArray.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
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

            var data = new MeshCacheEntry()
            {
                TextureTypes = new[] {textureArrayElement.SubmeshData.TextureArrayType}, // We have only one single entry per world mesh chunk.
                MaterialGroup = textureArrayElement.SubmeshData.Material.Group,
                Vertices = mesh.vertices,
                TriangleCount = mesh.triangles.Length, // We never ever reuse triangles. We can therefore simply store the length of array and later recreate it with Range(0,n).
                UVs = uvs.ToArray(),
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

            var uvs = new List<Vector4>();
            mesh.GetUVs(0, uvs);

            var data = new MeshCacheEntry()
            {
                TextureTypes = textureArrayElement.TextureTypes.ToArray(),
                MaterialGroup = MaterialGroup.Undefined, // Not needed for VOBs
                Vertices = mesh.vertices,
                TriangleCount = mesh.triangles.Length, // We never ever reuse triangles. We can therefore simply store the length of array and later recreate it with Range(0,n).
                UVs = uvs.ToArray(),
                Colors = mesh.colors32, // TODO - Do we use colors or colors32 for World and/or VOBs?
            };

            return data;
        }

        private async Task CreateFromCache(GameObject parentGo, CacheEntry entry)
        {
            var go = new GameObject(entry.Name);
            go.transform.SetParent(parentGo.transform);
            go.transform.SetLocalPositionAndRotation(entry.LocalPosition, entry.LocalRotation);

            // Unity's JsonSerialize will (mostly) always store empty classes with default values. We therefore check if MeshData has no triangles (aka is NULL).
            if (entry.MeshData != null && entry.MeshData.TriangleCount != 0)
            {
                var mesh = new Mesh();
                mesh.vertices = entry.MeshData.Vertices;
                mesh.triangles = Enumerable.Range(0, entry.MeshData.TriangleCount).ToArray(); // Create entries like: [0, 1, 2, ..., n-1)
                mesh.SetUVs(0, entry.MeshData.UVs);
                mesh.colors32 = entry.MeshData.Colors;

                var meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var meshRenderer = go.AddComponent<MeshRenderer>();
                GameGlobals.TextureArray.AssignTextureArray(entry, meshRenderer);
            }

            foreach (var child in entry.Children)
            {
                await CreateFromCache(go, child);
            }
        }

        private async Task SaveCacheFile(TextureArrayContainer textureData, CacheContainer worldData, CacheContainer vobsData, string fileName)
        {
            var textureJson = JsonUtility.ToJson(textureData);
            var worldJson = JsonUtility.ToJson(worldData);
            var vobsJson = JsonUtility.ToJson(vobsData);

            await WriteCompressedData(textureJson, BuildFilePathName(fileName, "textureData.json.gz"));
            await WriteCompressedData(worldJson, BuildFilePathName(fileName, "worldMeshes.json.gz"));
            await WriteCompressedData(vobsJson, BuildFilePathName(fileName, "vobMeshes.json.gz"));
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

        private string ReadCompressedData(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                return reader.ReadToEnd();
            }
        }


        private string BuildFilePathName(string fileName, string fileEnding)
        {
            return $"{_cacheRootFolderPath}/{fileName}.{fileEnding}";
        }
    }
}
