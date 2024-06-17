using System.IO;
using System.Linq;
using GUZ.Core.Context;
using GUZ.Core.Debugging;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Editor.Tools
{
    public class ContextTool
    {
        private const string HVR_COMPILER_FLAG = "GUZ_HVR_INSTALLED";

        [MenuItem("Gothic-UnZENity/Context/Check HVR status", priority = 1)]
        private static void CheckHVRPluginStatus()
        {
            var hvrFolder = Application.dataPath + "/HurricaneVR";
            var hvrExists = Directory.Exists(hvrFolder) && Directory.EnumerateFiles(hvrFolder).Count() != 0;
            var hvrCompilerSettingExists = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Contains(HVR_COMPILER_FLAG);
            bool hvrSceneSetting = GameObject.FindObjectOfType<FeatureFlags>()?.gameControls == GUZContext.Controls.VR_HVR;

            var message =
                $"Plugin installed: {hvrExists}\n" +
                $"Include in Build: {hvrCompilerSettingExists}\n" +
                $"Include in Scene: {hvrSceneSetting}";

            EditorUtility.DisplayDialog("Hurricane VR - Status", message, "Close");
        }

        /// <summary>
        /// Activate compiler flag being used inside GvrContext classes. This decides whether to build or don't build HVR classes.
        /// </summary>
        [MenuItem("Gothic-UnZENity/Context/Activate HVR in Build", priority = 2)]
        private static void ActivatePlugin()
        {
            // Change controls in FeatureFlags
            var featureFlags = GameObject.FindObjectOfType<FeatureFlags>();

            if (featureFlags != null)
                featureFlags.gameControls = GUZContext.Controls.VR_HVR;

            // Change compile flag in PlayerSettings
            var settings = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)
                .Split(";")
                .ToList();

            if (settings.Any(i => i.Equals(HVR_COMPILER_FLAG)))
                return;

            settings.Add(HVR_COMPILER_FLAG);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, string.Join(";", settings));
        }

        /// <summary>
        /// Deactivate compiler flag being used inside GvrContext classes. This decides whether to build or don't build HVR classes.
        /// </summary>
        [MenuItem("Gothic-UnZENity/Context/De-activate HVR", priority = 3)]
        private static void DeactivatePlugin()
        {
            // Change controls in FeatureFlags
            var featureFlags = GameObject.FindObjectOfType<FeatureFlags>();

            if (featureFlags != null)
                featureFlags.gameControls = GUZContext.Controls.VR_XRIT;

            // Change compile flag in PlayerSettings
            var settings = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)
                .Split(";")
                .ToList();

            if (!settings.Any(i => i.Equals(HVR_COMPILER_FLAG)))
                return;

            settings.Remove(HVR_COMPILER_FLAG);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, string.Join(";", settings));
        }
    }
}
