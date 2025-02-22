using System.Collections.Generic;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Extensions;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GUZ.Core.UI
{
    /// <summary>
    /// Alter font of Text based on G1 default/highlight fonts.
    /// </summary>
    public class UIEvents : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _elementsToFilter = new();
        [SerializeField] private AudioSource _audioSource;


        private static AudioClip _uiHover;
        private static AudioClip _uiClick;
        private static AudioClip _uiReturnClick;

        private void Awake()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(OnZenKitInitialized);
        }

        private void OnZenKitInitialized()
        {
            // Set sound files for button clicks initially.
            if (_uiHover == null)
            {
                _uiHover = SoundCreator.ToAudioClip("inv_change");
                _uiClick = SoundCreator.ToAudioClip("inv_open");
                _uiReturnClick = SoundCreator.ToAudioClip("inv_close");
            }
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
                _elementsToFilter.ForEach(i => i.GetComponentInChildren<TMP_Text>().spriteAsset = GameGlobals.Font.DefaultSpriteAsset);
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
                
                SetHighlightFont(hoveredObj.GetComponentInChildren<TMP_Text>());
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

                SetDefaultFont(hoveredObj.GetComponentInChildren<TMP_Text>());
            }
        }

        private void SetHighlightFont(TMP_Text textComp)
        {
            if (textComp == null)
            {
                return;
            }

            // Font has default value assigned and somehow Unity marks spriteAsset as null in this case.
            if (textComp.spriteAsset == null)
            {
                textComp.spriteAsset = GameGlobals.Font.HighlightSpriteAsset;
            }
            else
            {
                SetFont(textComp, textComp.spriteAsset.name.RemoveEnd(".fnt") + "_hi");
            }
        }

        /// <summary>
        /// On certain conditions (like SetEnabled(false), the appropriate OnPointerExit() won't be recognized.
        /// Let's do it manually.
        /// </summary>
        public static void SetDefaultFontsForChildren(GameObject root)
        {
            foreach (var textComp in root.GetComponentsInChildren<TMP_Text>())
            {
                SetDefaultFont(textComp);
            }
        }

        private static void SetDefaultFont(TMP_Text textComp)
        {
            if (textComp == null)
            {
                return;
            }

            // Font has default value assigned and somehow Unity marks spriteAsset as null in this case.
            if (textComp.spriteAsset == null)
            {
                textComp.spriteAsset = GameGlobals.Font.DefaultSpriteAsset;
            }
            else
            {
                SetFont(textComp, textComp.spriteAsset.name.RemoveEnd("_hi.fnt"));
            }
        }

        private static void SetFont(TMP_Text textComp, string fontName)
        {
            var newFont = GameGlobals.Font.TryGetFont(fontName);

            if (newFont == null)
            {
                Debug.LogWarning($"Font {newFont} not found.");
                return;
            }

            textComp.spriteAsset = newFont;
        }

        public void OnButtonClicked()
        {
            if (_audioSource == null)
            {
                Debug.LogWarning("AudioSource isn't set on UIEvents.cs - Therefore no menu button click could be played.");
                return;
            }

            _audioSource.PlayOneShot(_uiClick);
        }

        public void OnButtonBackClicked()
        {
            if (_audioSource == null)
            {
                Debug.LogWarning("AudioSource isn't set on UIEvents.cs - Therefore no menu button click could be played.");
                return;
            }

            _audioSource.PlayOneShot(_uiReturnClick);
        }

        private void OnDestroy()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.RemoveListener(OnZenKitInitialized);
        }
    }
}
