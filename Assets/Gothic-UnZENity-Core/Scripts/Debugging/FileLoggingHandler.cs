using System;
using System.IO;
using GUZ.Core.Manager.Settings;
using UnityEngine;

namespace GUZ.Core.Debugging
{
    public class FileLoggingHandler
    {
        private StreamWriter _fileWriter;
        private LogLevel _logLevel;
        private GameSettings _settings;

        // Levels are aligned with Unity's levels: UnityEngine.LogType
        private enum LogLevel
        {
            Debug = 0,
            Warning = 1,
            Error = 2,
            Exception = 3
        }

        public FileLoggingHandler(GameSettings settings)
        {
            _settings = settings;
        }

        public void Init()
        {
            Application.logMessageReceived += HandleLog;

            _fileWriter = new StreamWriter(Application.persistentDataPath + "/gothic-unzenity_log.txt", false);
            _fileWriter.WriteLine("DeviceModel: " + SystemInfo.deviceModel);
            _fileWriter.WriteLine("DeviceType: " + SystemInfo.deviceType);
            _fileWriter.WriteLine("OperatingSystem: " + SystemInfo.operatingSystem);
            _fileWriter.WriteLine("OperatingSystemFamily: " + SystemInfo.operatingSystemFamily);
            _fileWriter.WriteLine("MemorySize: " + SystemInfo.systemMemorySize);
            _fileWriter.WriteLine("GUZ Version: " + Application.version);
            _fileWriter.WriteLine();
            _fileWriter.Flush();

            if (Enum.TryParse(_settings.LogLevel, true, out LogLevel value))
            {
                _fileWriter.WriteLine("LogLevel Setting found: " + _settings.LogLevel);
                _logLevel = value;
            }
            else
            {
                _fileWriter.WriteLine("LogLevel Setting not found. Setting Default to >Warning<.");
                _logLevel = LogLevel.Warning;
            }

            _fileWriter.Flush();
        }

        public void Destroy()
        {
            Application.logMessageReceived -= HandleLog;
            _fileWriter.Close();
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!ShallBeLogged(type))
            {
                return;
            }

            _fileWriter.WriteLine(type + ": " + logString);
            _fileWriter.Flush();
        }

        private bool ShallBeLogged(LogType type)
        {
            switch (_logLevel)
            {
                case LogLevel.Debug:
                    return true; // Log everything
                case LogLevel.Warning:
                    return type != LogType.Log; // includes Assert, Warning, Error, Exception
                case LogLevel.Error:
                    return type == LogType.Error || type == LogType.Exception;
                case LogLevel.Exception:
                    return type == LogType.Exception;
                default:
                    return true; // never reached, but compiler demands it.
            }
        }
    }
}
