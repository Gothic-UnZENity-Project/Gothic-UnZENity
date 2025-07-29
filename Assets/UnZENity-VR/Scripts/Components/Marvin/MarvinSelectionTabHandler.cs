using System.Collections.Generic;
using GUZ.Core;
using GUZ.Core.Marvin;
using UnityEngine;

namespace GUZ.VR.Components.Marvin
{
    public class MarvinSelectionTabHandler : MonoBehaviour
    {
        
        
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
            var allProperties = new List<object>();
            
            foreach (var collector in propertyCollectors)
            {
                var properties = collector.CollectProperties();
                allProperties.AddRange(properties);
            }

            CreateFields();
        }

        private void CreateFields()
        {
            
        }
    }
}
