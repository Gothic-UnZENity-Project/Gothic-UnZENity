using System.Collections;
using GUZ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.VR.Components.Marvin
{
    public class MarvinLogsTabHandler : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private TMP_FontAsset _fontAsset;

        private void Start()
        {
            AddTextItem("Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo Foo");
            AddTextItem("Bar");
            AddTextItem("Baz");
            
            StartCoroutine(Render());
        }
        
        // Whenever we add elements, we need to re-enable the view to have Unity render items.
        private IEnumerator Render()
        {
            yield return new WaitForSeconds(1f);
            _contentContainer.gameObject.SetActive(false);
            _contentContainer.gameObject.SetActive(true);
        }
        
        
        private void AddTextItem(string text)
        {
            var item = ResourceLoader.TryGetPrefabObject(PrefabType.UiText, name: "ScrollItem",
                parent: _contentContainer.gameObject);

            var tmpText = item.GetComponent<TMP_Text>();
            tmpText.text = text;
            tmpText.font = _fontAsset;
            var preferredSize = tmpText.GetPreferredValues(text);

            var rectTransform = item.GetComponent<RectTransform>();
            rectTransform.sizeDelta = preferredSize;
            
            tmpText.textWrappingMode = TextWrappingModes.Normal;
            tmpText.overflowMode = TextOverflowModes.Overflow;

            // Enlarge content view to show horizontal scroll bar if the entry is bigger than the last one.
            if (_contentContainer.rect.width < preferredSize.x)
                _contentContainer.sizeDelta = new Vector2(preferredSize.x,  _contentContainer.rect.height);
        }
    }
}
