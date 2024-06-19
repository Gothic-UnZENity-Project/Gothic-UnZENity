using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace GUZ.Core.Manager.Settings
{
    [System.Serializable]
    public class GameSettings
    {
        private const string SETTINGS_FILE_NAME = "GameSettings.json";
        private const string SETTINGS_FILE_NAME_DEV = "GameSettings.dev.json";

        public string GothicIPath;
        public string GothicILanguage;
        public string LogLevel;

        public string GothicMenuFontPath;
        public string GothicSubtitleFontPath;

        public Dictionary<string, Dictionary<string, string>> GothicINISettings = new();
        
        public bool CheckIfGothic1InstallationExists()
        {
            var g1DataPath = Path.GetFullPath(Path.Join(GothicIPath, "Data"));
			var g1WorkPath = Path.GetFullPath(Path.Join(GothicIPath, "_work"));

            return Directory.Exists(g1WorkPath) && Directory.Exists(g1DataPath);
        }
        
        public static void SaveGameSettings(GameSettings gameSettings)
        {
            if (Application.platform == RuntimePlatform.Android) return;
            var settingsFilePath = $"{GetRootPath()}/{SETTINGS_FILE_NAME}";
            var settingsJson = JsonUtility.ToJson(gameSettings, true);
            File.WriteAllText(settingsFilePath,settingsJson);
        }

        public static GameSettings Load()
        {
            var rootPath = GetRootPath();

            var settingsFilePath = $"{rootPath}/{SETTINGS_FILE_NAME}";
            if (!File.Exists(settingsFilePath))
            {
                if (Application.platform == RuntimePlatform.Android)
                    CopyGameSettingsForAndroidBuild();
                else
                    throw new ArgumentException($"No >GameSettings.json< file exists at >{settingsFilePath}<. Can't load Gothic1.");
            }

            var settingsJson = File.ReadAllText(settingsFilePath);
            var obj = JsonUtility.FromJson<GameSettings>(settingsJson);

            // We ignore the "GothicIPath" field which is found in GameSettings for Android
            if (Application.platform == RuntimePlatform.Android)
            {
                obj.GothicIPath = rootPath;
            }

            var settingsDevFilePath = $"{rootPath}/{SETTINGS_FILE_NAME_DEV}";
            if (File.Exists(settingsDevFilePath))
            {
                var devJson = File.ReadAllText(settingsDevFilePath);
                JsonUtility.FromJsonOverwrite(devJson, obj);
            }

            var iniFilePath = Path.Combine(obj.GothicIPath, "system", "gothic.ini");
            if (!File.Exists(iniFilePath))
            {
                Debug.Log("The gothic.ini file does not exist at the specified path :" + iniFilePath);
                return obj;
            }

            obj.GothicINISettings = ParseGothicINI(iniFilePath);
            return obj;
        }

        /// <summary>
        /// Return path of settings file based on target architecture.
        /// As there is no "folder" for an Android build (as it's a packaged .apk file), we need to check within user directory.
        /// </summary>
        /// <returns></returns>
        private static string GetRootPath()
        {
            if (Application.platform == RuntimePlatform.Android)
                // https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
                // Will be: /storage/emulated/<userid>/Android/data/<packagename>/files
                return Application.persistentDataPath;
            else
                // https://docs.unity3d.com/ScriptReference/Application-streamingAssetsPath.html
                // Will be:
                // 1. Editor: Assets\StreamingAssets\
                // 2. Standalone: Build\Gothic-UnZENity_Data\StreamingAssets\
                return Application.streamingAssetsPath;
        }

        /// <summary>
        /// Import the settings file from streamingAssetPath to persistentDataPath.
        /// Since the settings file is in streamingAssetPath, we need to use UnityWebRequest to move it so we can have access to it
        /// as detailed here https://docs.unity3d.com/ScriptReference/Application-streamingAssetsPath.html
        /// </summary>
        private static void CopyGameSettingsForAndroidBuild()
        {
            string GameSettingsPath = Path.Combine(Application.streamingAssetsPath, $"{SETTINGS_FILE_NAME}");
            string result = "";
            if (GameSettingsPath.Contains("://") || GameSettingsPath.Contains(":///"))
            {
                UnityWebRequest www = UnityWebRequest.Get(GameSettingsPath);
                www.SendWebRequest();
                // Wait until async download is done
                while (!www.isDone) { }
                result = www.downloadHandler.text;
            }
            else
                result = File.ReadAllText(GameSettingsPath);

            string FinalPath = Path.Combine(Application.persistentDataPath, $"{SETTINGS_FILE_NAME}");
            File.WriteAllText(FinalPath, result);
        }

        private static Dictionary<string, Dictionary<string, string>> ParseGothicINI(string filePath)
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = null;

            foreach (var line in File.ReadLines(filePath))
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    data[currentSection] = new Dictionary<string, string>();
                }
                else
                {
                    var keyValue = trimmedLine.Split(new char[] { '=' }, 2);
                    if (keyValue.Length == 2 && currentSection != null)
                    {
                        data[currentSection][keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }

            return data;
        }
    }
}
