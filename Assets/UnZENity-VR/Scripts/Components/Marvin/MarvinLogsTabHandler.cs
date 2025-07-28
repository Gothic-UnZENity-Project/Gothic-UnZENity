using System;
using System.Collections;
using GUZ.Core;
using GUZ.Core.Util;
using TMPro;
using UberLogger;
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
        [SerializeField] private RectTransform _categoryContentContainer;

        
        [SerializeField] private ScrollRect _logsRoot;
        [SerializeField] private RectTransform _logContentContainer;
        
        [SerializeField] private Texture2D _messageIcon;
        [SerializeField] private Texture2D _warningIcon;
        [SerializeField] private Texture2D _errorIcon;
        
        private const int _paddingTop = 10;
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
        [UnityEditor.InitializeOnLoadMethod]
        static void InitializeEditor()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                // Find and remove this logger from UberLogger
                Logger.RemoveLogger(typeof(MarvinLogsTabHandler).FullName);
            };
        }
#endif

        private void FillCategories()
        {
            var catIndex = 0;
            foreach (var category in Enum.GetNames(typeof(LogCat)))
            {
                var categoryGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiDebugButton, name: category, parent: _categoryContentContainer.gameObject);
                var categoryTransform = categoryGo.GetComponent<RectTransform>();
                categoryTransform.localPosition = new Vector2(0, -_paddingTop - (_logLineHeight / 2f) - _logLineHeight * 2 * catIndex);
                categoryTransform.sizeDelta = new Vector2(_categoriesRoot.rect.width, _logLineHeight * 2);
                
                var textComp =  categoryGo.GetComponentInChildren<TMP_Text>();
                textComp.text = category;
                catIndex++;
            }

            _categoryContentContainer.sizeDelta = new Vector2(_categoryContentContainer.sizeDelta.x, _paddingTop + (_logLineHeight / 2f) + _logLineHeight * 2 * catIndex);
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
            
            var textComp = itemGo.GetComponentInChildren<TMP_Text>();
            var textRectComp = textComp.GetComponent<RectTransform>();
            var preferredSize = textComp.GetPreferredValues(logInfo.Message); // Calculate size for message based on 
            textComp.text = logInfo.Message;
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
    }
}
