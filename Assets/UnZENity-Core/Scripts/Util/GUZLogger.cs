using System;
using System.Diagnostics;
using UberLogger;

namespace GUZ.Core.Util
{
    public static class GUZLogger
    {
        public enum LogModule
        {
            Ai,
            Animations,
            Dialog,
            DxMusic,
            Npc,
            PreCaching,
            ZenKit,
        }

        [StackTraceIgnore]
        public static void Log(string message, LogModule module)
        {
            UberDebug.LogChannel(module.ToString(), message);
        }

        [StackTraceIgnore]
        public static void LogWarning(string message, LogModule module)
        {
            UberDebug.LogWarningChannel(module.ToString(), message);
        }

        [StackTraceIgnore]
        public static void LogError(string message, LogModule module)
        {
            UberDebug.LogErrorChannel(module.ToString(), message);
        }


        [StackTraceIgnore]
        [Conditional("UNITY_EDITOR")]
        public static void LogEditor(string message, LogModule module)
        {
            Log(message, module);
        }

        [StackTraceIgnore]
        [Conditional("UNITY_EDITOR")]
        public static void LogWarningEditor(string message, LogModule module)
        {
            LogWarning(message, module);
        }

        [StackTraceIgnore]
        [Conditional("UNITY_EDITOR")]
        public static void LogErrorEditor(string message, LogModule module)
        {
            LogError(message, module);
        }
    }
}
