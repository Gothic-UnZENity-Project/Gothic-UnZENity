using System.Text;
using GUZ.Core.Const;
using GUZ.Core.Util;
using UnityEngine.Localization.Settings;
using ZenKit;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Manager
{
    public class LocalizationManager
    {
        private const string _localizationStringTable = "UnZENity-UI";
        
        public void SetLanguage(string language, StringEncoding encoding)
        {
            Logger.Log($"Selecting StringEncoding={encoding}, Language={language}", LogCat.Loading);

            // ZenKit
            StringEncodingController.SetEncoding(encoding);
            
            // GameData
            GameData.Encoding = Encoding.GetEncoding((int)encoding);
            GameData.Language = language;
            
            // Unity.Localization
            var locale = LocalizationSettings.AvailableLocales.GetLocale(language);

            if (!locale)
            {
                Logger.LogWarning($"Language Language={language} not found. Using en as fallback.", LogCat.Loading);
                locale = LocalizationSettings.ProjectLocale; // Fallback: en
            }
            
            LocalizationSettings.SelectedLocale = locale;
        }
        
        public string GetText(string key)
        {
            // DEBUG - Fetch all key strings + key ids
            // LocalizationSettings.StringDatabase.GetAllTables().WaitForCompletion()[0].SharedData
            
            return LocalizationSettings.StringDatabase.GetLocalizedString(_localizationStringTable, key);
        }
    }
}
