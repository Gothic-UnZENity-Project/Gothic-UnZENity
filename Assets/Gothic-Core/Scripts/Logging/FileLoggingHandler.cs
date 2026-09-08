using Gothic.Core.Models.Config;
#if !UNITY_EDITOR
using System;
using System.Linq;
using UberLogger;
using UnityEngine;
#endif

namespace Gothic.Core.Logging
{
    public class FileLoggingHandler
    {
#if UNITY_EDITOR
        public void Init(JsonRootConfig _)
        {
            // Disabled in Editor.
        }

        public void Destroy()
        {
            // Disabled in Editor.
        }
#else
        private FileLoggingLogger _logger;

        private const string _logFileName = "Gothic-Unity.log.txt";
        
        public FileLoggingHandler()
        {
            // As we need to get logs as early as possible, we will pre-initialize it immediately.
            PreInit();
        }

        private void PreInit()
        {
            string rootFolder;
            if (Application.platform == RuntimePlatform.Android)
            {
                rootFolder = Application.persistentDataPath;
            }
            else
            {
                rootFolder = Application.persistentDataPath;
            }

            _logger = new FileLoggingLogger($"{rootFolder}/{_logFileName}", false);
            UberLogger.Logger.AddLogger(_logger);

            _logger.WriteLine("DeviceModel: " + SystemInfo.deviceModel);
            _logger.WriteLine("DeviceType: " + SystemInfo.deviceType);
            _logger.WriteLine("OperatingSystem: " + SystemInfo.operatingSystem);
            _logger.WriteLine("OperatingSystemFamily: " + SystemInfo.operatingSystemFamily);
            _logger.WriteLine("MemorySize: " + SystemInfo.systemMemorySize);
            _logger.WriteLine("Gothic Unity Version: " + Application.version);
            _logger.WriteLine(string.Empty);
        }

        public void Init(JsonRootConfig rootConfig)
        {
            LogSeverity logLevel;

            if (Enum.TryParse(rootConfig.LogLevel, true, out LogSeverity value))
            {
                _logger.WriteLine("LogLevel Setting found: " + rootConfig.LogLevel);
                logLevel = value;
            }
            else
            {
                _logger.WriteLine("LogLevel Setting not found. Setting Default to >Warning<.");
                logLevel = LogSeverity.Warning;
            }

            string[] logCategories;
            if (rootConfig.LogCategories != null)
            {
                logCategories = rootConfig.LogCategories.Split(',', ';')
                    // We sanitize possible case issues as we want to have string channels for a small performance benefit when logging at runtime.
                    .Select(i => Enum.TryParse(i.Trim(), true, out LogCat cat) ? cat.ToString() : null)
                    .Where(i => i != null)
                    .ToArray();
            }
            else
            {
                logCategories = Array.Empty<string>();
            }

            if (logCategories.Length == 0)
            {
                var enumValues = Enum.GetValues(typeof(LogCat)).Cast<LogCat>().Select(v => v.ToString());
                _logger.WriteLine($"No valid category found. Activating logs for all categories: [{string.Join(';', enumValues)}]");
            }
            else
            {
                _logger.WriteLine($"Log Categories found and activated: [{string.Join(';', logCategories)}]");
            }
            _logger.WriteLine(string.Empty);

            UberLogger.Logger.AddFilter(new FileLoggingFilter(logLevel, logCategories));
        }

        public void Destroy()
        {
            _logger = null;
        }

#endif
    }
}
