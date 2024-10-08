﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using ZenKit;

namespace GUZ.Core.Manager.Settings
{
    [Serializable]
    public class GameSettings
    {
        private const string _settingsFileName = "GameSettings.json";
        private const string _settingsFileNameDev = "GameSettings.dev.json";
        private const string _defaultSteamGothic1Folder = @"C:\Program Files (x86)\Steam\steamapps\common\Gothic\";
        private const string _defaultSteamGothic2Folder = @"C:\Program Files (x86)\Steam\steamapps\common\Gothic II\";



        public string Gothic1Path;
        public string Gothic2Path;
        public string LogLevel;

        public Dictionary<string, string> GothicIniSettings = new();

        public string IniSkyDayColor(int index) => GothicIniSettings.TryGetValue($"zDayColor{index}", out var value) ? value : "0 0 0";
        public bool IniPlayLogoVideos => GothicIniSettings.TryGetValue("playLogoVideos", out var value) ? Convert.ToBoolean(Convert.ToInt16(value)) : true;
        public string IniPlayerInstanceName => GothicIniSettings.GetValueOrDefault("playerInstanceName", "PC_HERO");

        public static GameSettings Load(GameVersion version)
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
            loadedSettings.Gothic1Path = AlterGothicInstallationPath(loadedSettings.Gothic1Path, GameVersion.Gothic1);
            loadedSettings.Gothic2Path = AlterGothicInstallationPath(loadedSettings.Gothic2Path, GameVersion.Gothic2);

            // FIXME - Need to be moved to a later stage. Right now we don't know which game version to load!
            LoadIniFile(loadedSettings, version);
            
            return loadedSettings;
        }

        /// <summary>
        /// We prepare use of app by copying GameSettings.json and empty Gothic installation directories where gamers
        /// will place their game data into.
        ///
        /// HINT: With Android 10+, there is no easy way to use data from a different folder. i.e. we can create files and folders wherever we want,
        /// but if we upload or alter them from another app (like SideQuest), we loose access (Androids new Scoped Storage/Shared Storage feature).
        /// Therefore, let's stick with the installation folder as it's the official place where other apps (SideQuest etc.) can update/upload our files
        /// and Gothic-UnZENity can still read the data later on.
        /// </summary>
        private static void PrepareAndroidFolders()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }
            
            // If directory exists and GameSettings.json is placed, we assume everything is created already.
            if (File.Exists($"{Application.persistentDataPath}/{_settingsFileName}"))
            {
                return;
            }
            
            // Create folder(s)
            Directory.CreateDirectory($"{Application.persistentDataPath}/Gothic1");
            Directory.CreateDirectory($"{Application.persistentDataPath}/Gothic2");
            
            // Copy GameSettings.json into app's shared folder
            var gameSettingsPath = Path.Combine($"{Application.streamingAssetsPath}/{_settingsFileName}");
            
            var www = UnityWebRequest.Get(gameSettingsPath);
            www.SendWebRequest();
            
            // Wait until async download is done
            while (!www.isDone)
            { }
            
            var result = www.downloadHandler.text;
            File.WriteAllText($"{Application.persistentDataPath}/{_settingsFileName}", result);

            // If existing, copy GameSettings.dev.json into writable shared storage folder of our app.
            var gameSettingsDevPath = Path.Combine($"{Application.streamingAssetsPath}/{_settingsFileNameDev}");
            if (File.Exists(gameSettingsDevPath))
            {
                var devresult = File.ReadAllText(gameSettingsPath);
                File.WriteAllText($"{Application.persistentDataPath}/{_settingsFileNameDev}", devresult);
            }
        }

        /// <summary>
        /// Return path of settings file based on target architecture.
        /// </summary>
        private static string GetGameSettingsRootPath()
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
        /// Check if the specified path inside GameSettings is a valid Gothic installation. If not, use a platform specific fallback:
        /// Standalone: C:\Program Files (x86)\Steam\steamapps\common\Gothic\
        /// Android: /storage/emulated/0/Android/data/com.GothicUnZENity/files/Gothic1/
        /// </summary>
        private static string AlterGothicInstallationPath(string gothicInstallationPath, GameVersion version)
        {
            // GameSettings (or its dev) entry already provides a valid installation directory.
            if (Directory.Exists(gothicInstallationPath))
            {
                return gothicInstallationPath;
            }
            
            // Try platform specific fallbacks.
            if (Application.platform == RuntimePlatform.Android)
            {
                if (version == GameVersion.Gothic1)
                {
                    return $"{Application.persistentDataPath}/Gothic1";
                }
                else
                {
                    return $"{Application.persistentDataPath}/Gothic2";
                }
            }
            // Standalone
            else
            {
                if (version == GameVersion.Gothic1)
                {
                    return _defaultSteamGothic1Folder;
                }
                else
                {
                    return _defaultSteamGothic2Folder;
                }
            }
        }

        public bool CheckIfGothicInstallationExists(GameVersion version)
        {
            var gothicRootPath = version == GameVersion.Gothic1 ? Gothic1Path : Gothic2Path;

            var gothicDataPath = $"{gothicRootPath}/Data";
            var gothicWorkPath = $"{gothicRootPath}/_work";

            return Directory.Exists(gothicWorkPath) && Directory.Exists(gothicDataPath);
        }

        private static void LoadIniFile(GameSettings loadedSettings, GameVersion version)
        {
            var gothicRoot = version == GameVersion.Gothic1 ? loadedSettings.Gothic1Path : loadedSettings.Gothic2Path;
            
            var iniFilePath = $"{gothicRoot}/system/Gothic.ini";
            if (!File.Exists(iniFilePath))
            {
                Debug.LogError("The gothic.ini file does not exist at the specified path :" + iniFilePath);
                return;
            }

            var data = new Dictionary<string, string>();

            foreach (var line in File.ReadLines(iniFilePath))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                {
                    continue;
                }

                // We don't need to store [section] information. Every property name is unique.
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    continue;
                }
                else
                {
                    var keyValue = trimmedLine.Split(new[] { '=' }, 2);
                    if (keyValue.Length == 2)
                    {
                        data[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }

            loadedSettings.GothicIniSettings = data;
        }
    }
}
