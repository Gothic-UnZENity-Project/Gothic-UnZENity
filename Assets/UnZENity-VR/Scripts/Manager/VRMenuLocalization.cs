using System.Collections.Generic;
using GUZ.Core.Globals;

namespace GUZ.VR.Manager
{
    public static class VRMenuLocalization
    {
        // This could be loaded from a JSON file instead. But is it worth the effort for such a small amount of entries?
        private static Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            { "en", new() {
                { "menuitem.vr_accessibility", "VR Accessibility" },
                { "menuitem.vr_accessibility.headline", "VR ACCESSIBILITY SETTINGS" },
                { "menuitem.moveDirection.label", "Move Direction" },
                { "menuitem.moveDirection.description", "" },
                { "menuitem.moveDirection.value", "camera|left controller|right controller" },
                { "menuitem.rotationType.label", "Rotation Type" },
                { "menuitem.rotationType.description", "" },
                { "menuitem.rotationType.value", "snap|smooth" },
                { "menuitem.snapRotation.label", "Snap Rotation" },
                { "menuitem.snapRotation.description", "Amount of Snap Rotation per click" },
                { "menuitem.snapRotation.value", "5°|10°|15°|20°|25°|30°|35°|40°|45°" },
                { "menuitem.smoothRotation.label", "Smooth Rotation Speed" },
                { "menuitem.smoothRotation.description", "" },
                { "menuitem.smooth.label", "Spectator Smoothing" },
                { "menuitem.smooth.description", "Set smoothing of spectator camera (PCVR only)" },
                { "menuitem.smooth.value", "off|low|medium|high" },
            }},
            { "de", new() {
                { "menuitem.vr_accessibility", "VR Barrierefreiheit" },
                { "menuitem.vr_accessibility.headline", "VR BARRIEREFREIHEIT EINSTELLUNGEN" },
                { "menuitem.moveDirection.label", "Bewegungsrichtung" },
                { "menuitem.moveDirection.description", "" },
                { "menuitem.moveDirection.value", "Kamera|linker Controller|rechter Controller" },
                { "menuitem.rotationType.label", "Rotationstyp" },
                { "menuitem.rotationType.description", "" },
                { "menuitem.rotationType.value", "snap|smooth" },
                { "menuitem.snapRotation.label", "Snap Rotation" },
                { "menuitem.snapRotation.description", "Stärke der Rotation pro Klick" },
                { "menuitem.snapRotation.value", "5°|10°|15°|20°|25°|30°|35°|40°|45°" },
                { "menuitem.smoothRotation.label", "Smooth Rotation" },
                { "menuitem.smoothRotation.description", "Geschwindingkeit der Smooth Rotation" },
                { "menuitem.smooth.label", "Spectator Glättung" },
                { "menuitem.smooth.description", "Glätten kleiner Bewegungsruckler des Spectators (nur PCVR)" },
                { "menuitem.smooth.value", "aus|gering|mittel|hoch" },
            }}
            // FIXME - Add other languages
            // cs, es, fr, it, pl, ru
        };
        
            
        public static string GetText(string key)
        {
            if (_translations.TryGetValue(GameData.Language, out var languageDict))
            {
                if (languageDict.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            // Fallback to English
            if (_translations.TryGetValue("en", out var englishDict))
            {
                if (englishDict.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            return $"KEY_NOT_FOUND:{key}";
        }
    }
}
