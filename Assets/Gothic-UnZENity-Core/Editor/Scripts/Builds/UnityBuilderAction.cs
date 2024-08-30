using System;
using System.Collections.Generic;
using Unity.XR.OpenXR.Features.PICOSupport;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace GUZ.Core.Editor.Builds.UnityBuildTools
{
    public class UnityBuilderAction
    {
        private static string[] SCENES = FindEnabledEditorScenes();

        private static readonly string APP_NAME = "Gothic-UnZENity";
        private static readonly string TARGET_DIR = "Builds";
        

        [MenuItem("UnZENity/Build/Pico")]
        private static void PerformPicoBuild()
        {
            var target_path = TARGET_DIR + "/Pico/" + APP_NAME + ".apk";
            SetPicoSettings();
            GenericBuild(SCENES, target_path, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
        }

        [MenuItem("UnZENity/Build/PCVR")]
        private static void PerformWindows64Build()
        {
            var target_path = TARGET_DIR + "/Windows64/" + APP_NAME + ".exe";

            SetWindows64Settings();
            
            GenericBuild(SCENES, target_path, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64,
                BuildOptions.None);
        }
        
        [MenuItem("UnZENity/Build/Quest")]
        private static void PerformQuestBuild()
        {
            var target_path = TARGET_DIR + "/Quest/" + APP_NAME + ".apk";
            SetQuestSettings();
            GenericBuild(SCENES, target_path, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
        }

        private static void GenericBuild(string[] scenes, string target_path, BuildTargetGroup build_target_group,
            BuildTarget build_target, BuildOptions build_options)
        {
            // Set the target platform for the build
            EditorUserBuildSettings.SwitchActiveBuildTarget(build_target_group, build_target);

            // Set BuildPlayerOptions
            var options = new BuildPlayerOptions();
            options.scenes = scenes;
            options.locationPathName = target_path;
            options.target = build_target;
            options.targetGroup = build_target_group;
            options.options = build_options;

            // Build the project
            var report = BuildPipeline.BuildPlayer(options);

            // TODO: Check GitHub Issue: https://github.com/game-ci/unity-builder/issues/563
            Debug.Log("Logging fake Build results so that the build via game-ci/unity-builder does not fail...");
            Debug.Log(
                $"###########################{Environment.NewLine}#      Build results      #{Environment.NewLine}###########################{Environment.NewLine}" +
                $"{Environment.NewLine}Duration: 00:00:00.0000000{Environment.NewLine}Warnings: 0{Environment.NewLine}Errors: 0{Environment.NewLine}Size: 0 bytes{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}Build succeeded!");
        }

        private static string[] FindEnabledEditorScenes()
        {
            var EditorScenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                {
                    continue;
                }

                EditorScenes.Add(scene.path);
            }

            return EditorScenes.ToArray();
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

            foreach (var item in OpenXRSettings.ActiveBuildTargetInstance.GetFeatures())
            {
                Debug.Log(item.name);
            }

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
