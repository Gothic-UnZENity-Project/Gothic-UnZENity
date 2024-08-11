using System.Collections.Generic;
using GUZ.Core.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GUZ.Core.UI
{
    /// <summary>
    /// Alter font of Text based on G1 default/highlight fonts.
    /// </summary>
    public class HoverEvent : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _elementsToFilter = new();


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
                
                var textComponent = hoveredObj.GetComponentInChildren<TMP_Text>();

                if (textComponent == null)
                {
                    continue;
                }
                
                textComponent.spriteAsset = GameGlobals.Font.HighlightSpriteAsset;
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
                
                var textComponent = hoveredObj.GetComponentInChildren<TMP_Text>();

                if (textComponent == null)
                {
                    continue;
                }

                textComponent.spriteAsset = GameGlobals.Font.DefaultSpriteAsset;
            }
        }
    }
}
