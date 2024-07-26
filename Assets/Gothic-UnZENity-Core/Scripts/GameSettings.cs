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
        private const string _androidProjectFolder = "/sdcard/Documents/Gothic-UnZENity-Data/";
        private const string _defaultSteamGothicFolder = @"C:\Program Files (x86)\Steam\steamapps\common\Gothic\";


        public string GothicIPath;
        public string LogLevel;

        public Dictionary<string, Dictionary<string, string>> GothicIniSettings = new();


        public static GameSettings Load()
        {
            PrepareAndroidFolders();
            
            var rootPath = GetGameSettingsRootPath();
            var settingsFilePath = $"{rootPath}/{_settingsFileName}";
            
            if (!File.Exists(settingsFilePath))
            {
                throw new ArgumentException($"No >GameSettings.json< file exists at >{settingsFilePath}<.");
            }

            var settingsJson = File.ReadAllText(settingsFilePath);
            var loadedSettings = JsonUtility.FromJson<GameSettings>(settingsJson);

            // Overwrite data with GameSettings.dev.json if it exists.
            var settingsDevFilePath = $"{rootPath}/{_settingsFileNameDev}";
            if (File.Exists(settingsDevFilePath))
            {
                var devJson = File.ReadAllText(settingsDevFilePath);
                JsonUtility.FromJsonOverwrite(devJson, loadedSettings);
            }

            // We need to do a final check for Gothic installation path and which one to ultimately use.
            loadedSettings.GothicIPath = AlterGothicInstallationPath(loadedSettings.GothicIPath);
            
            var iniFilePath = Path.Combine(loadedSettings.GothicIPath, "system", "gothic.ini");
            if (!File.Exists(iniFilePath))
            {
                Debug.LogError("The gothic.ini file does not exist at the specified path :" + iniFilePath);
                return loadedSettings;
            }

            loadedSettings.GothicIniSettings = ParseGothicIni(iniFilePath);
            return loadedSettings;
        }

        /// <summary>
        /// We create some ready-to use folder inside /sdcard/Documents/Gothic-UnZENity/ to provide folders
        /// where gamers will place their game data into.
        /// </summary>
        private static void PrepareAndroidFolders()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }
            
            // If directory exists and GameSettings.json is placed, we assume everything is created already.
            if (Directory.Exists(_androidProjectFolder) && File.Exists($"{_androidProjectFolder}/{_settingsFileName}"))
            {
                return;
            }
            
            // Create folder
            Directory.CreateDirectory(_androidProjectFolder);
            Directory.CreateDirectory($"{_androidProjectFolder}/Gothic1");
            
            // Copy GameSettings.json into writable Documents folder
            var gameSettingsPath = Path.Combine($"{Application.streamingAssetsPath}/{_settingsFileName}");
            
            var www = UnityWebRequest.Get(gameSettingsPath);
            www.SendWebRequest();
            
            // Wait until async download is done
            while (!www.isDone)
            { }
            
            var result = www.downloadHandler.text;
            File.WriteAllText($"{_androidProjectFolder}/{_settingsFileName}", result);

            // If existing, copy GameSettings.dev.json into writable Documents folder
            var gameSettingsDevPath = Path.Combine($"{Application.streamingAssetsPath}/{_settingsFileNameDev}");
            if (File.Exists(gameSettingsDevPath))
            {
                var devresult = File.ReadAllText(gameSettingsPath);
                File.WriteAllText($"{_androidProjectFolder}/{_settingsFileNameDev}", devresult);
            }
        }

        /// <summary>
        /// Return path of settings file based on target architecture.
        /// </summary>
        private static string GetGameSettingsRootPath()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                // We already extracted the necessary data into this folder before.
                return _androidProjectFolder;
            }
            else // Standalone
            {
                // https://docs.unity3d.com/ScriptReference/Application-streamingAssetsPath.html
                // This will be:
                //   1. Editor: Assets\StreamingAssets\
                //   2. Standalone: Build\Gothic-UnZENity_Data\StreamingAssets\
                return Application.streamingAssetsPath;
            }
        }

        /// <summary>
        /// Check if the specified path inside GameSettings is a valid Gothic installation. If not, use a platform specific fallback:
        /// Standalone: C:\Program Files (x86)\Steam\steamapps\common\Gothic\
        /// Android: /sdcard/Documents/Gothic-UnZENity/Gothic1/
        /// </summary>
        private static string AlterGothicInstallationPath(string gothicInstallationPath)
        {
            if (Directory.Exists(gothicInstallationPath))
            {
                return gothicInstallationPath;
            }
            
            // Try platform specific fallbacks.
            if (Application.platform == RuntimePlatform.Android)
            {
                return $"{_androidProjectFolder}/Gothic1";
            }
            // Standalone
            else
            {
                return _defaultSteamGothicFolder;
            }
        }
        
        public bool CheckIfGothic1InstallationExists()
        {
            var g1DataPath = Path.GetFullPath(Path.Join(GothicIPath, "Data"));
            var g1WorkPath = Path.GetFullPath(Path.Join(GothicIPath, "_work"));

            return Directory.Exists(g1WorkPath) && Directory.Exists(g1DataPath);
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
