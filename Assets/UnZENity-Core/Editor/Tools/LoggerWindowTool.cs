using System;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using UnityEditor;

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
                var editorLogWindow = UberLogger.Logger.GetLogger<UberLoggerEditor>();
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

        [MenuItem(itemName: "UnZENity/Debug/Daedalus StackTrace", priority = 2)]
        public static void PrintStackTrace()
        {
            if (GameData.GothicVm == null)
                Logger.LogErrorEditor("No Gothic VM initialized. Please start the game first.", LogCat.Debug);
            
            GameData.GothicVm?.PrintStackTrace();
        }
    }
}
