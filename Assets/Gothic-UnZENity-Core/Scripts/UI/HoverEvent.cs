using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GUZ.Core.UI
{
    public class HoverEvent : MonoBehaviour
    {
        public void OnPointerEnter(BaseEventData evt)
        {
            if (evt is not PointerEventData pointerEventData)
            {
                return;
            }

            foreach (var hoveredObj in pointerEventData.hovered)
            {
                var textComponent = hoveredObj.GetComponent<TMP_Text>();

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
                var textComponent = hoveredObj.GetComponent<TMP_Text>();

                if (textComponent == null)
                {
                    continue;
                }

                textComponent.spriteAsset = GameGlobals.Font.DefaultSpriteAsset;
            }
        }
    }
}
