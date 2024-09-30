using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ZenKit;
using CompressionLevel = System.IO.Compression.CompressionLevel;

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
            // FIXME Hardcoded G1
            _cacheRootFolderPath = $"{Application.persistentDataPath}/Cache/Gothic1/";
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

        private async Task SaveCacheFile(TextureArrayContainer textureData, CacheContainer worldData, CacheContainer vobsData, string fileName)
        {
            // var stream = new FileStream(BuildFilePathName(fileName, ".json"), FileMode.CreateNew);

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


        private string BuildFilePathName(string fileName, string fileEnding)
        {
            return $"{_cacheRootFolderPath}/{fileName}.{fileEnding}";
        }
    }
}
