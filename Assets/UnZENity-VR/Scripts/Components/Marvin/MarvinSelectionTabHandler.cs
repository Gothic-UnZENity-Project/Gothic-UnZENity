using System.Collections.Generic;
using GUZ.Core;
using GUZ.Core.Marvin;
using GUZ.Core.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Components.Marvin
{
    public class MarvinSelectionTabHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Text _objectTextComp;
        [SerializeField] private RectTransform _contentTransform;
        
        private const int _propertyHeight = 15;
        private const int _propertyLabelWidth = 50;
        private List<object> _marvinProperties;
        private int _propertyCount;

        
        private void Update()
        {
            if (!GameGlobals.Marvin.IsMarvinSelectionMode)
                return;

            FillMarvinSelection();
        }

        private void FillMarvinSelection()
        {
            var go = GameGlobals.Marvin.MarvinSelectionGO;

            // Wait until an object is selected.
            if (go == null)
                return;

            // Reset first. If we have errors below to ensure normal gameplay is reactivated.
            GameGlobals.Marvin.IsMarvinSelectionMode = false;

            var propertyCollectors = go.GetComponentsInChildren<IMarvinPropertyCollector>();
            _marvinProperties = new();
            
            foreach (var collector in propertyCollectors)
            {
                var properties = collector.CollectProperties();
                _marvinProperties.AddRange(properties);
            }

            CreateFields();
        }

        private void CreateFields()
        {
            _objectTextComp.text = GameGlobals.Marvin.MarvinSelectionGO.name;
            
            _propertyCount = 0;
            foreach (var property in _marvinProperties)
            {
                switch (property)
                {
                    case MarvinPropertyHeader header:
                        CreateHeader(header);
                        break;
                    case MarvinProperty<bool> boolProperty:
                        CreateField(boolProperty);
                        break;
                    case MarvinProperty<int> intProperty:
                        CreateField(intProperty);
                        break;
                    case MarvinProperty<float> floatProperty:
                        CreateField(floatProperty);
                        break;
                    default:
                        Logger.LogWarning($"Unhandled MarvinSelection property type: {property.GetType()}", LogCat.Debug);
                        break;
                }
            }
        }

        private void CreateHeader(MarvinPropertyHeader header)
        {
            // Add additional free line above.
            _propertyCount++;
            
            var headerGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiDebugText, name: header.Name, parent: _contentTransform.gameObject);
            headerGo.GetComponentInChildren<TMP_Text>().text = header.Name;
            
            var headerTransform = headerGo!.GetComponent<RectTransform>();
            headerTransform.localPosition = new Vector2(0, -(_propertyHeight / 2f) - _propertyHeight * _propertyCount);
            headerTransform.sizeDelta = new Vector2(_propertyLabelWidth, _propertyHeight);

            _propertyCount++;
        }

        private GameObject CreateField(MarvinProperty<bool> boolProperty)
        {
            var toggleGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiDebugToggle, name: boolProperty.Name, parent: _contentTransform.gameObject);
            toggleGo.GetComponentInChildren<TMP_Text>().text = boolProperty.Name;
            
            var toggleTransform = toggleGo!.GetComponent<RectTransform>();
            toggleTransform.localPosition = new Vector2(0, -(_propertyHeight / 2f) - _propertyHeight * _propertyCount);
            toggleTransform.sizeDelta = new Vector2(_propertyLabelWidth, _propertyHeight);

            var toggleComp = toggleGo.GetComponentInChildren<Toggle>();
            toggleComp.isOn = boolProperty.Getter();
            toggleComp.onValueChanged.AddListener(value => boolProperty.Setter(value));
            
            _propertyCount++;
            return toggleGo;
        }
        
        private GameObject CreateField(MarvinProperty<int> intProperty)
        {
            var sliderGo = CreateField(new MarvinProperty<float>(intProperty.Name, () => (float)intProperty.Getter(), value => intProperty.Setter((int)value), intProperty.MinValue, intProperty.MaxValue));

            // The only difference to float handling!
            sliderGo.GetComponentInChildren<Slider>().wholeNumbers = true;

            return sliderGo;
        }

        private GameObject CreateField(MarvinProperty<float> floatProperty)
        {
            var sliderGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiDebugSlider, name: floatProperty.Name, parent: _contentTransform.gameObject);
            sliderGo.GetComponentInChildren<TMP_Text>().text = floatProperty.Name;

            var sliderTransform = sliderGo!.GetComponent<RectTransform>();
            sliderTransform.localPosition = new Vector2(0, -(_propertyHeight / 2f) - _propertyHeight * _propertyCount);
            sliderTransform.sizeDelta = new Vector2(_propertyLabelWidth, _propertyHeight);
            
            var sliderComp = sliderGo.GetComponentInChildren<Slider>();
            sliderComp.minValue = floatProperty.MinValue;
            sliderComp.maxValue = floatProperty.MaxValue;

            sliderComp.wholeNumbers = false;
            sliderComp.value = floatProperty.Getter();
            sliderComp.onValueChanged.AddListener(value => floatProperty.Setter(value));
            
            _propertyCount++;
            return sliderGo;
        }
    }
}
