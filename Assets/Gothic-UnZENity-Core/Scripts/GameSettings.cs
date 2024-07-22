using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace GUZ.Core.Manager.Settings
{
    [Serializable]
    public class GameSettings
    {
        private const string _settingsFileName = "GameSettings.json";
        private const string _settingsFileNameDev = "GameSettings.dev.json";

        public string GothicIPath;
        public string LogLevel;

        public Dictionary<string, Dictionary<string, string>> GothicIniSettings = new();

        public bool CheckIfGothic1InstallationExists()
        {
            var g1DataPath = Path.GetFullPath(Path.Join(GothicIPath, "Data"));
            var g1WorkPath = Path.GetFullPath(Path.Join(GothicIPath, "_work"));

            return Directory.Exists(g1WorkPath) && Directory.Exists(g1DataPath);
        }

        public static void SaveGameSettings(GameSettings gameSettings)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return;
            }

            var settingsFilePath = $"{GetRootPath()}/{_settingsFileName}";
            var settingsJson = JsonUtility.ToJson(gameSettings, true);
            File.WriteAllText(settingsFilePath, settingsJson);
        }

        public static GameSettings Load()
        {
            var rootPath = GetRootPath();

            var settingsFilePath = $"{rootPath}/{_settingsFileName}";
            if (!File.Exists(settingsFilePath))
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    CopyGameSettingsForAndroidBuild();
                }
                else
                {
                    throw new ArgumentException(
                        $"No >GameSettings.json< file exists at >{settingsFilePath}<. Can't load Gothic1.");
                }
            }

            var settingsJson = File.ReadAllText(settingsFilePath);
            var obj = JsonUtility.FromJson<GameSettings>(settingsJson);

            // We ignore the "GothicIPath" field which is found in GameSettings for Android
            if (Application.platform == RuntimePlatform.Android)
            {
                obj.GothicIPath = rootPath;
            }

            var settingsDevFilePath = $"{rootPath}/{_settingsFileNameDev}";
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

            obj.GothicIniSettings = ParseGothicIni(iniFilePath);
            return obj;
        }

        /// <summary>
        /// Return path of settings file based on target architecture.
        /// As there is no "folder" for an Android build (as it's a packaged .apk file), we need to check within user directory.
        /// </summary>
        /// <returns></returns>
        private static string GetRootPath()
        {
            // https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
            // Will be: /storage/emulated/<userid>/Android/data/<packagename>/files
            if (Application.platform == RuntimePlatform.Android)
            {
                return Application.persistentDataPath;
            }

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
            var gameSettingsPath = Path.Combine(Application.streamingAssetsPath, $"{_settingsFileName}");
            var result = "";
            if (gameSettingsPath.Contains("://") || gameSettingsPath.Contains(":///"))
            {
                var www = UnityWebRequest.Get(gameSettingsPath);
                www.SendWebRequest();
                // Wait until async download is done
                while (!www.isDone)
                {
                }

                result = www.downloadHandler.text;
            }
            else
            {
                result = File.ReadAllText(gameSettingsPath);
            }

            var finalPath = Path.Combine(Application.persistentDataPath, $"{_settingsFileName}");
            File.WriteAllText(finalPath, result);
        }

        private static Dictionary<string, Dictionary<string, string>> ParseGothicIni(string filePath)
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = null;

            foreach (var line in File.ReadLines(filePath))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    data[currentSection] = new Dictionary<string, string>();
                }
                else
                {
                    var keyValue = trimmedLine.Split(new[] { '=' }, 2);
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
