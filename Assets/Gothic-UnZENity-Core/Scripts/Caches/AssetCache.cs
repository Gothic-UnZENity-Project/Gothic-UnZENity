using System;
using System.Collections.Generic;
using System.IO;
using GUZ.Core.Data;
using GUZ.Core.Globals;
using JetBrains.Annotations;
using ZenKit.Daedalus;

namespace GUZ.Core.Caches
{
    public static class AssetCache
    {
        private static readonly Dictionary<string, ItemInstance> ItemDataCache = new();
        private static readonly Dictionary<int, SvmInstance> SvmDataCache = new();
        private static readonly Dictionary<string, SoundEffectInstance> SfxDataCache = new();
        private static readonly Dictionary<string, ParticleEffectInstance> PfxDataCache = new();

        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit.
        /// There are two ways of getting Item data. Via INSTANCE name or symbolIndex inside VM.
        /// </summary>
        public static ItemInstance TryGetItemData(int instanceId)
        {
            var symbol = GameData.GothicVm.GetSymbolByIndex(instanceId);

            if (symbol == null)
                return null;

            return TryGetItemData(symbol.Name);
        }

        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit.
        /// There are two ways of getting Item data. Via INSTANCE name or symbolIndex inside VM.
        /// </summary>
        [CanBeNull]
        public static ItemInstance TryGetItemData(string key)
        {
            var preparedKey = GetPreparedKey(key);
            if (ItemDataCache.TryGetValue(preparedKey, out var data))
                return data;

            ItemInstance newData = null;
            try
            {
                newData = GameData.GothicVm.InitInstance<ItemInstance>(preparedKey);
            }
            catch (Exception)
            {
                // ignored
            }
            ItemDataCache[preparedKey] = newData;

            return newData;
        }
        
        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit.
        /// </summary>
        [CanBeNull]
        public static SvmInstance TryGetSvmData(int voiceId)
        {
            if (SvmDataCache.TryGetValue(voiceId, out var data))
                return data;

            SvmInstance newData = null;
            try
            {
                newData = GameData.GothicVm.InitInstance<SvmInstance>($"SVM_{voiceId}");
            }
            catch (Exception)
            {
                // ignored
            }
            SvmDataCache[voiceId] = newData;

            return newData;
        }

        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit and don't need to be deleted during runtime.
        /// </summary>
        [CanBeNull]
        public static SoundEffectInstance TryGetSfxData(string key)
        {
            var preparedKey = GetPreparedKey(key);
            if (SfxDataCache.TryGetValue(preparedKey, out var data))
                return data;

            SoundEffectInstance newData = null;
            try
            {
                newData = GameData.SfxVm.InitInstance<SoundEffectInstance>(preparedKey);
            }
            catch (Exception)
            {
                // ignored
            }
            SfxDataCache[preparedKey] = newData;

            return newData;
        }

        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit and don't need to be deleted during runtime.
        /// </summary>
        public static ParticleEffectInstance TryGetPfxData(string key)
        {
            var preparedKey = GetPreparedKey(key);
            if (PfxDataCache.TryGetValue(preparedKey, out var data))
                return data;

            ParticleEffectInstance newData = null;
            try
            {
                newData = GameData.PfxVm.InitInstance<ParticleEffectInstance>(preparedKey);
            }
            catch (Exception)
            {
                // ignored
            }
            PfxDataCache[preparedKey] = newData;

            return newData;
        }

        private static string GetPreparedKey(string key)
        {
            var lowerKey = key.ToLower();
            var extension = Path.GetExtension(lowerKey);

            if (extension == string.Empty)
                return lowerKey;
            else
                return lowerKey.Replace(extension, "");
        }

        public static void Dispose()
        {
            ItemDataCache.Clear();
            SvmDataCache.Clear();
            SfxDataCache.Clear();
            PfxDataCache.Clear();
        }
    }
}
