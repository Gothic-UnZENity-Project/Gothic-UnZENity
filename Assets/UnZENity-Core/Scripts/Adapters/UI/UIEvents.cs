﻿using System.Collections.Generic;
using GUZ.Core.Logging;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Services.UI;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.Core.Adapters.UI
{
    /// <summary>
    /// Alter font of Text based on G1 default/highlight fonts.
    /// </summary>
    public class UIEvents : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _elementsToFilter = new();
        [SerializeField] private AudioSource _audioSource;

        [Inject] private readonly UIEventsService _uiEventsService;
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly FontService _fontService;

        private static AudioClip _uiHover;
        private static AudioClip _uiClick;
        private static AudioClip _uiReturnClick;

        private void Awake()
        {
            if (GameContext.IsZenKitInitialized && _uiHover == null)
                InitializeAudio();
        }

        private void InitializeAudio()
        {
            // Set sound files for button clicks initially.
            _uiHover = _audioService.CreateAudioClip("inv_change");
            _uiClick = _audioService.CreateAudioClip("inv_open");
            _uiReturnClick = _audioService.CreateAudioClip("inv_close");
        }

        /// <summary>
        /// Add filter which elements should be marked "hovered" when pointed towards as
        /// HoverEvents can become quite eager to fire events for too many elements.
        /// </summary>
        public void SetElementsToHover(List<GameObject> elements, bool resetHover = false)
        {
            _elementsToFilter = elements;

            // Reset fonts of all items. Useful if previously hovered elements will be toggled in visibility and highlighted again when going "Back".
            if (resetHover)
            {
                _elementsToFilter.ForEach(i => i.GetComponentInChildren<TMP_Text>().spriteAsset = _fontService.DefaultSpriteAsset);
            }
        }
        
        public void OnPointerEnter(BaseEventData evt)
        {
            var elementFound = false;

            if (evt is not PointerEventData pointerEventData)
            {
                return;
            }
            
            foreach (var hoveredObj in pointerEventData.hovered)
            {
                if (!_elementsToFilter.IsEmpty() && !_elementsToFilter.Contains(hoveredObj))
                {
                    continue;
                }
                
                _uiEventsService.SetHighlightFont(hoveredObj.GetComponentInChildren<TMP_Text>());
                elementFound = true;
            }
            
            if (elementFound)
            {
                _audioSource.PlayOneShot(_uiHover);
            }
        }

        public void OnPointerExit(BaseEventData evt)
        {
            if (evt is not PointerEventData pointerEventData)
            {
                return;
            }
            
            foreach (var hoveredObj in pointerEventData.hovered)
            {
                if (!_elementsToFilter.IsEmpty() && !_elementsToFilter.Contains(hoveredObj))
                {
                    continue;
                }

                _uiEventsService.SetDefaultFont(hoveredObj.GetComponentInChildren<TMP_Text>());
            }
        }

        public void OnButtonClicked()
        {
            if (_audioSource == null)
            {
                Logger.LogWarning("AudioSource isn't set on UIEvents.cs - Therefore no menu button click could be played.", LogCat.Ui);
                return;
            }

            _audioSource.PlayOneShot(_uiClick);
            _uiEventsService.SetDefaultFont(GetComponentInChildren<TMP_Text>());
        }

        public void OnButtonBackClicked()
        {
            if (_audioSource == null)
            {
                Logger.LogWarning("AudioSource isn't set on UIEvents.cs - Therefore no menu button click could be played.", LogCat.Ui);
                return;
            }

            _audioSource.PlayOneShot(_uiReturnClick);
        }

        private void OnDestroy()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.RemoveListener(InitializeAudio);
        }
    }
}
