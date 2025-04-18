using System;
using GUZ.Core.Util;
using UberLogger;
using UnityEditor;

namespace GUZ.Core.Editor.Tools
{
    public class LoggerWindowTool
    {
        [MenuItem(itemName: "UnZENity/Debug/Uber Console", priority = 1)]
        public static void ShowUberLoggerWindow()
        {
            UberLoggerEditorWindow.Init();

            var editorLogWindow = Logger.GetLogger<UberLoggerEditor>();

            if (editorLogWindow == null)
                return;

            editorLogWindow.InitializeChannels(Enum.GetNames(typeof(GUZLogger.LogModule)));

        }
    }
}
