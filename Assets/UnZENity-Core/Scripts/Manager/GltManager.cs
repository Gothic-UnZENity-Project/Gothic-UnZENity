using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Export;
using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.Core.Manager
{
    /// <summary>
    /// Manager to store and retrieve glTF data.
    /// glTF == GL Transmission Format
    /// </summary>
    public class GltManager
    {
        private string _gltCacheFilePath;
        private string _gltCacheFileEnding = "glb";

        public void Init()
        {
            _gltCacheFilePath = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/";
        }

        public bool DoesCacheFileExist(string fileName)
        {
            return File.Exists(BuildFilePathName(fileName));
        }

        private string BuildFilePathName(string fileName)
        {
            return $"{_gltCacheFilePath}/{fileName}.{_gltCacheFileEnding}";
        }

        public async Task SaveGlt(GameObject worldRootGo, GameObject vobsRootGo, string fileName)
        {
            PreSaveCheck(worldRootGo);
            PreSaveCheck(vobsRootGo);

            // Create a new export settings instance
            var exportSettings = new ExportSettings()
            {
                Format = GltfFormat.Binary, // If you want to test something change to GltFormat.Json (e.g. check for extracted texture.jpgs and object structure)
                ComponentMask = ComponentType.Mesh,
                FileConflictResolution = FileConflictResolution.Overwrite
            };

            var export = new GameObjectExport(exportSettings);

            // A scene is a structure for glTF to structure GameObjects. We will use it to separate VOBs and World meshes.
            export.AddScene(new []{worldRootGo}, "WorldCache");
            export.AddScene(new []{vobsRootGo}, "VobCache");

            try
            {
                Directory.CreateDirectory(_gltCacheFilePath);

                var success = await export.SaveToFileAndDispose(BuildFilePathName(fileName));
                if (!success)
                {
                    Debug.LogError("Something went wrong exporting the World+VOB glTF");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw new Exception("Error exporting glTF", e);
            }
        }

        public async Task LoadGlt(GameObject rootGo, string fileName)
        {
            var filePathName = BuildFilePathName(fileName);

            var gltf = new GltfImport();
            var loading = gltf.LoadFile(filePathName);
            while (!loading.IsCompleted)
            {
                await Task.Yield();
            }

            // Success == Result == true
            if (loading.Result)
            {
                for (var i = 0; i < gltf.SceneCount; i++)
                {
                    var loadingWorld = gltf.InstantiateSceneAsync(rootGo.transform, i);
                    while (!loadingWorld.IsCompleted)
                    {
                        await Task.Yield();
                    }
                }
            }
        }

        /// <summary>
        /// We want to store meshes only. We therefore need to check if textures are also included and raise an exception if so.
        /// Textures should always be created via TextureArray. This pre-check is a good performance check if something is wrong with mesh generation in general.
        /// </summary>
        private void PreSaveCheck(GameObject rootGo)
        {
            var renderers = rootGo.GetComponentsInChildren<MeshRenderer>(true);

            var allowedShaderNames = new string[]
            {
                // TODO - This shader is used for materials used from e.g. HurricaneVR (chest and the inventory rings inside).
                // TODO   We can safely remove this once we load only Gothic data without prefabs at cache time.
                Constants.ShaderLit.name,
                Constants.ShaderWorldLitName
            };

            var nonTextureArrayMeshes = renderers
                .Where(i => i.materials.Length != 1 || !allowedShaderNames.Contains(i.material.shader.name))
                .ToList();

            if (nonTextureArrayMeshes.Any())
            {
                Debug.LogError("Bug: Meshes are loaded which should be inside texture array instead of loading textures now. We don't want to have these textures (*.jpg) inside cache and stop now!");
                foreach (var invalidElement in nonTextureArrayMeshes)
                {
                    Debug.LogError($"Invalid mesh where texture is assigned during caching time: {invalidElement.name}", invalidElement);
                }
                throw new ArgumentException("See Error log");
            }
        }
    }
}
