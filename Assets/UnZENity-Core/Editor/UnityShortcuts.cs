using UnityEditor;

namespace GUZ.Core.Editor
{
    public class UnityShortcuts
    {
        [MenuItem("UnZENity/Unity/Build Settings", priority = 100)]
        public static void ShowBuildSettingsWindow()
        {
            EditorApplication.ExecuteMenuItem("File/Build Settings...");
        }
        
        [MenuItem("UnZENity/Unity/Build And Run", priority = 110)]
        public static void ShowBuildAndRunWindow()
        {
            EditorApplication.ExecuteMenuItem("File/Build And Run");
        }

        [MenuItem("UnZENity/Unity/Preferences", priority = 200)]
        public static void ShowPreferencesWindow()
        {
            EditorApplication.ExecuteMenuItem("Edit/Preferences...");
        }

        [MenuItem("UnZENity/Unity/Project Settings", priority = 210)]
        public static void ShowProjectSettingsWindow()
        {
            EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
        }

        [MenuItem("UnZENity/Unity/Package Manager", priority = 300)]
        public static void ShowPackageManagerWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }
    }
}
