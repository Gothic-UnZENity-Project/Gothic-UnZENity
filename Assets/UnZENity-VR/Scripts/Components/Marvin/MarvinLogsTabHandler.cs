using System;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Util;
using TMPro;
using UberLogger;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ILogger = UberLogger.ILogger;
using Logger = UberLogger.Logger;
using PrefabType = GUZ.Core.PrefabType;

namespace GUZ.VR.Components.Marvin
{
    public class MarvinLogsTabHandler : MonoBehaviour, ILogger
    {
        [SerializeField] private RectTransform _categoriesRoot;
        
        [SerializeField] private ScrollRect _logsRoot;
        [SerializeField] private RectTransform _logContentContainer;
        
        [SerializeField] private Texture2D _messageIcon;
        [SerializeField] private Texture2D _warningIcon;
        [SerializeField] private Texture2D _errorIcon;
        
        private const int _logLineHeight = 15;
        private int _logCount;
        
        private void Start()
        {
            Logger.AddLogger(this);
            
            // TODO - Really needed?
            _logsRoot.normalizedPosition = new Vector2(0, 0);

            FillCategories();

            // Debug
            // Logger.LogWarning("SPAM", LogCat.Ai);
            // Logger.LogWarning("SPAM", LogCat.Ai);
            // Logger.LogWarning("SPAM", LogCat.Ai);
            // Logger.LogError("SPAM-Error", LogCat.Debug);
            // Logger.LogError("SPAM-Error", LogCat.Debug);
            // Logger.LogError("SPAM-Error", LogCat.Debug);
        }
      
#if UNITY_EDITOR
        /// <summary>
        /// We need to disable Logging (and especially creating new GameObjects) when we recompile at runtime (Editor only).
        /// Otherwise, Unity will crash.
        /// </summary>
        [InitializeOnLoadMethod]
        static void InitializeEditor()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                // Find and remove this logger from UberLogger
                Logger.RemoveLogger(typeof(MarvinLogsTabHandler).FullName);
            };
        }
#endif

        private void FillCategories()
        {
            var defaultCategories = new[] { "Nothing", "Everything", "-No Channel-" };
            
            var catIndex = 0;
            foreach (var category in defaultCategories.Concat(Enum.GetNames(typeof(LogCat))))
            {
                var categoryGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiDebugButton, name: category, parent: _categoriesRoot.gameObject);
                var categoryTransform = categoryGo!.GetComponent<RectTransform>();
                categoryTransform.localPosition = new Vector2(0, -(_logLineHeight / 2f) - _logLineHeight * catIndex);
                categoryTransform.sizeDelta = new Vector2(_categoriesRoot.rect.width - 25f, _logLineHeight);
                
                var textComp =  categoryGo.GetComponentInChildren<TMP_Text>();
                textComp.text = category;
                catIndex++;

                // One empty line to separate "special" categories from others.
                if (category == "-No Channel-")
                    catIndex++;
            }
        }

        public void Log(LogInfo logInfo)
        {
            AddTextItem(logInfo);
        }
        
        private void AddTextItem(LogInfo logInfo)
        {
            var itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiDebugLogLine, name: "LogItem", parent: _logContentContainer.gameObject);
            var itemTransform = itemGo.GetComponent<RectTransform>();
            itemTransform.localScale = Vector3.one; // 0 when instantiating.
            itemTransform.anchorMin = new Vector2(0, 1);
            itemTransform.anchorMax = new Vector2(0, 1);
            // Offset: Half of text height - itemIndex*itemHeight
            itemTransform.localPosition = new Vector2(0, -(_logLineHeight / 2f) - _logLineHeight * _logCount);

            var message = FormatLogText(logInfo);
            
            var textComp = itemGo.GetComponentInChildren<TMP_Text>();
            var textRectComp = textComp.GetComponent<RectTransform>();
            var preferredSize = textComp.GetPreferredValues(message); // Calculate size for message based on 
            textComp.text = message;
            textRectComp.sizeDelta = new Vector2(preferredSize.x, _logLineHeight);
            textRectComp.localPosition = new Vector2(preferredSize.x / 2, 0);

            var image = itemGo.GetComponentInChildren<RawImage>();
            var imageRectComp = image.GetComponent<RectTransform>();
            imageRectComp.localPosition = new Vector2(10, 0);
            image.texture = logInfo.Severity switch
            {
                LogSeverity.Message => _messageIcon,
                LogSeverity.Warning => _warningIcon,
                LogSeverity.Error => _errorIcon,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            // Enlarge content view to show horizontal scroll bar if the entry is bigger than the last one.
            if (_logContentContainer.rect.width < preferredSize.x)
                _logContentContainer.sizeDelta = new Vector2(preferredSize.x, _logContentContainer.rect.height);

            // Height: Half of text height (for offset) + itemCount*itemHeight
            _logContentContainer.sizeDelta = new Vector2(_logContentContainer.sizeDelta.x, (_logLineHeight / 2f) + (_logCount + 1) * _logLineHeight);
            _logCount++;
        }

        /// <summary>
        /// Format the message as follows:
        ///     [channel] 0.000 : message  <-- Both channel and time shown
        ///     0.000 : message            <-- Time shown, channel hidden
        ///     [channel] : message        <-- Channel shown, time hidden
        ///     message                    <-- Both channel and time hidden
        /// </summary>
        private string FormatLogText(LogInfo log)
        {
            var showChannel = !string.IsNullOrEmpty(log.Channel);
            var channelMessage = showChannel ? string.Format("[{0}]", log.Channel) : "";
            var channelTimeSeparator = showChannel ? " " : "";
            var timeMessage = string.Format("{0}", log.GetRelativeTimeStampAsString());
            var prefixMessageSeparator = " : ";
            
            return string.Format("{0}{1}{2}{3}{4}",
                channelMessage,
                channelTimeSeparator,
                timeMessage,
                prefixMessageSeparator,
                log.Message
            );
        }
    }
}
