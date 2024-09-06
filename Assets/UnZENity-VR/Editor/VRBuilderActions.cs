using System.Collections.Generic;
using Unity.XR.OpenXR.Features.PICOSupport;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace GUZ.VR.Editor
{
    public class VRBuilderActions
    {
        private static readonly string[] _scenes = FindEnabledEditorScenes();

        private const string _appName = "Gothic-UnZENity";
        private const string _targetDir = "Builds";


        [MenuItem("UnZENity/Build/Pico")]
        private static void PerformPicoBuild()
        {
            var targetPath = _targetDir + "/Pico/" + _appName + ".apk";
            SetPicoSettings();
            GenericBuild(_scenes, targetPath, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
        }

        [MenuItem("UnZENity/Build/PCVR")]
        private static void PerformWindows64Build()
        {
            var targetPath = _targetDir + "/Windows64/" + _appName + ".exe";

            SetWindows64Settings();
            
            GenericBuild(_scenes, targetPath, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64,
                BuildOptions.None);
        }
        
        [MenuItem("UnZENity/Build/Quest")]
        private static void PerformQuestBuild()
        {
            var target_path = _targetDir + "/Quest/" + _appName + ".apk";
            SetQuestSettings();
            GenericBuild(_scenes, target_path, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
        }

        private static void GenericBuild(string[] scenes, string targetPath, BuildTargetGroup buildTargetGroup,
            BuildTarget buildTarget, BuildOptions buildOptions)
        {
            // Set the target platform for the build
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

            // Set BuildPlayerOptions
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = targetPath,
                target = buildTarget,
                targetGroup = buildTargetGroup,
                options = buildOptions
            };

            // Build the project
            BuildPipeline.BuildPlayer(options);
        }

        /// <summary>
        /// Used to import HurricaneVR which was downloaded before.
        /// </summary>
        public static void ImportPackage(string packagePath)
        {
            AssetDatabase.ImportPackage(packagePath, false);
        }

        private static string[] FindEnabledEditorScenes()
        {
            var editorScenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                {
                    continue;
                }

                editorScenes.Add(scene.path);
            }

            return editorScenes.ToArray();
        }

        private static void SetWindows64Settings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }
        
        private static void SetPicoSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            //Enable Pico
            OpenXRSettings.ActiveBuildTargetInstance.GetFeature<PICO4ControllerProfile>().enabled = true;
            OpenXRSettings.ActiveBuildTargetInstance.GetFeature<PICOFeature>().enabled = true;

            //Disable Meta
            OpenXRSettings.ActiveBuildTargetInstance.GetFeature<MetaQuestFeature>().enabled = false;
            // OpenXRSettings.ActiveBuildTargetInstance.GetFeature<MetaQuestTouchProControllerProfile>().enabled = false;
            OpenXRSettings.ActiveBuildTargetInstance.GetFeature<OculusTouchControllerProfile>().enabled = false;

            
            Debug.Log("OpenXR settings set for: Pico");
        }

        private static void SetQuestSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            
            //Enable Meta
            // OpenXRSettings.ActiveBuildTargetInstance.GetFeature<MetaQuestTouchProControllerProfile>().enabled = true;
            OpenXRSettings.ActiveBuildTargetInstance.GetFeature<OculusTouchControllerProfile>().enabled = true;
            OpenXRSettings.ActiveBuildTargetInstance.GetFeature<MetaQuestFeature>().enabled = true;

            //Disable Pico
            OpenXRSettings.ActiveBuildTargetInstance.GetFeature<PICOFeature>().enabled = false;
            OpenXRSettings.ActiveBuildTargetInstance.GetFeature<PICO4ControllerProfile>().enabled = false;

            Debug.Log("OpenXR settings set for: Quest");
        }
    }
}
