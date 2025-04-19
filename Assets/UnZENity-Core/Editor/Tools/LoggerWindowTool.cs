using System;
using GUZ.Core.Util;
using UnityEditor;
using Logger = UberLogger.Logger;

namespace GUZ.Core.Editor.Tools
{
    // GUZ - Provide UnZENity an event to fetch when UberLogger-Channels should be added.
    [InitializeOnLoad]
    public static class LoggerWindowTool
    {

        static LoggerWindowTool()
        {
            UberLoggerEditorWindow.OnEnableWindow.AddListener(() =>
            {
                var editorLogWindow = Logger.GetLogger<UberLoggerEditor>();
                if (editorLogWindow == null)
                    return;

                editorLogWindow.InitializeChannels(Enum.GetNames(typeof(LogCat)));
            });
        }

        [MenuItem(itemName: "UnZENity/Debug/Uber Console", priority = 1)]
        public static void ShowUberLoggerWindow()
        {
            UberLoggerEditorWindow.Init();
        }
    }
}
