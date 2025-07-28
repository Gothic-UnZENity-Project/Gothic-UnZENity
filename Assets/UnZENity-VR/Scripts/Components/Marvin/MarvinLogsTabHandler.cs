using System;
using System.Collections;
using GUZ.Core;
using GUZ.Core.Util;
using TMPro;
using UberLogger;
using UnityEngine;
using UnityEngine.UI;
using ILogger = UberLogger.ILogger;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Components.Marvin
{
    public class MarvinLogsTabHandler : MonoBehaviour, ILogger
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private TMP_FontAsset _fontAsset;
        
        [SerializeField] private Texture2D _messageIcon;
        [SerializeField] private Texture2D _warningIcon;
        [SerializeField] private Texture2D _errorIcon;
        
        private const int _logLineHeight = 15;
        private int _lastLogPosition = 0;
        
        private void Start()
        {
            UberLogger.Logger.AddLogger(this);
            
            // Debug
            // Logger.LogWarning("SPAM", LogCat.Ai);
            // Logger.LogWarning("SPAM", LogCat.Ai);
            // Logger.LogWarning("SPAM", LogCat.Ai);
            // Logger.LogError("SPAM-Error", LogCat.Debug);
            // Logger.LogError("SPAM-Error", LogCat.Debug);
            // Logger.LogError("SPAM-Error", LogCat.Debug);
        }
        
        public void Log(LogInfo logInfo)
        {
            AddTextItem(logInfo);

            StartCoroutine(Render());
        }
        
        // Whenever we add elements, we need to re-enable the view to have Unity render items.
        private IEnumerator Render()
        {
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentContainer);
            _scrollRect.normalizedPosition = new Vector2(0, 0);
        }
        
        private void AddTextItem(LogInfo logInfo)
        {
            var itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiLogLine, name: "LogItem", parent: _contentContainer.gameObject);
            var itemTransform = itemGo.GetComponent<RectTransform>();
            itemTransform.localScale = Vector3.one; // 0 when instantiating.
            itemTransform.anchorMin = new Vector2(0, 1);
            itemTransform.anchorMax = new Vector2(0, 1);
            // Offset: Half of text height - itemIndex*itemHeight
            itemTransform.localPosition = new Vector2(0, -(_logLineHeight / 2f) - _logLineHeight * _lastLogPosition);
            
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
            if (_contentContainer.rect.width < preferredSize.x)
                _contentContainer.sizeDelta = new Vector2(preferredSize.x, _contentContainer.rect.height);

            // Height: Half of text height (for offset) + itemCount*itemHeight
            _contentContainer.sizeDelta = new Vector2(_contentContainer.sizeDelta.x, (_logLineHeight / 2f) + (_lastLogPosition + 1) * _logLineHeight);
            _lastLogPosition++;
        }
    }
}
