using UnityEngine;
using ZenKit;

namespace GUZ.Core.Util
{
    public static class Logging
    {
        public static void OnZenKitLogMessage(LogLevel level, string name, string message)
        {
            // Using the fastest string concatenation as we might have a lot of logs here.
            var messageString = string.Concat("level=", level, ", name=", name, ", message=", message);

            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(messageString);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(messageString);
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                case LogLevel.Trace:
                    Debug.Log(messageString);
                    break;
            }
        }

        public static void OnDirectMusicLogMessage(DirectMusic.LogLevel level, string message)
        {
            // Using the fastest string concatenation as we might have a lot of logs here.
            var messageString = string.Concat("level=", level, ", message=", message);

            switch (level)
            {
                case DirectMusic.LogLevel.Error:
                    Debug.LogError(messageString);
                    break;
                case DirectMusic.LogLevel.Warning:
                    Debug.LogWarning(messageString);
                    break;
                case DirectMusic.LogLevel.Info:
                case DirectMusic.LogLevel.Debug:
                case DirectMusic.LogLevel.Trace:
                    Debug.Log(messageString);
                    break;
            }
        }
    }
}
