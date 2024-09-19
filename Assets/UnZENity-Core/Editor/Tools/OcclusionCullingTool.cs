using System;
using GUZ.Core.Creator;
using GUZ.Core.Globals;
using GUZ.Core.Manager.Settings;
using UnityEditor;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Editor.Tools
{
    /// HOW TO USE:
    /// 1. LOAD THE SCENE FOR WHICH YOU WANT OCCLUSION CULLING
    /// 2. RUN UnZENity/Tools/Load world meshes in editor
    /// 3. Window/Rendering/Occlusion Culling
    /// 4. BAKE THE OCCLUSION CULLING
    /// 5. SAVE THE SCENE
    public class OcclusionCullingTool : EditorWindow
    {
        private static IntPtr _vfsPtr = IntPtr.Zero;

        [MenuItem("UnZENity/Tools/Load world meshes in editor", true)]
        private static bool ValidateMyMenuItem()
        {
            // If game is in playmode, disable button.
            return !EditorApplication.isPlaying;
        }

        [MenuItem("UnZENity/Tools/Load world meshes in editor")]
        public static void ShowWindow()
        {
            // Do not show Window when game is started.
            if (Application.isPlaying)
            {
                return;
            }

            // TODO - We need to add a popup to decide which Game Version to load.
            var settings = GameSettings.Load(GameVersion.Gothic1);
            
            // TODO - Add parameter to switch between G1 and G2
            ResourceLoader.Init(settings.Gothic1Path);

            WorldCreator.LoadEditorWorld();
        }

        private void OnGUI()
        {
            // Do not show Window when game is started.
            if (Application.isPlaying)
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            GameData.Dispose();
        }
    }
}
