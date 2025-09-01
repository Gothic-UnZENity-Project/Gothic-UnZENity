using System;
using System.Collections.Generic;
using System.IO;
using GUZ.Core.Data.Adapter;
using GUZ.Core.Globals;
using JetBrains.Annotations;
using ZenKit.Daedalus;

namespace GUZ.Core.Vm
{
    public static class VmInstanceManager
    {
        private static readonly Dictionary<string, ItemInstance> _itemDataCache = new();
        private static readonly Dictionary<string, FightAiAdapter> _fightAiDataCache = new();
        private static readonly Dictionary<int, SvmInstance> _svmDataCache = new();
        private static readonly Dictionary<string, SfxAdapter> _sfxDataCache = new();
        private static readonly Dictionary<string, ParticleEffectInstance> _pfxDataCache = new();

        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit.
        /// There are two ways of getting Item data. Via INSTANCE name or symbolIndex inside VM.
        /// </summary>
        public static ItemInstance TryGetItemData(int instanceId)
        {
            var symbol = GameData.GothicVm.GetSymbolByIndex(instanceId);

            if (symbol == null)
            {
                return null;
            }

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
            if (_itemDataCache.TryGetValue(preparedKey, out var data))
            {
                return data;
            }

            ItemInstance newData = null;
            try
            {
                newData = GameData.GothicVm.InitInstance<ItemInstance>(preparedKey);
            }
            catch (Exception)
            {
                // ignored
            }

            _itemDataCache[preparedKey] = newData;

            return newData;
        }

        public static FightAiAdapter TryGetFightAiData(string nameTemplate, int instanceId)
        {
            var preparedKey = GetPreparedKey(string.Format(nameTemplate, instanceId));
            
            if (_fightAiDataCache.TryGetValue(preparedKey, out var data))
                return data;

            FightAiAdapter newData = null;
            try
            {
                newData = new FightAiAdapter(GameData.FightVm.InitInstance<FightAiInstance>(preparedKey));
            }
            catch (Exception)
            {
                // ignored
            }

            _fightAiDataCache[preparedKey] = newData;

            return newData;
        }
        
        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit.
        /// </summary>
        [CanBeNull]
        public static SvmInstance TryGetSvmData(int voiceId)
        {
            if (_svmDataCache.TryGetValue(voiceId, out var data))
            {
                return data;
            }

            SvmInstance newData = null;
            try
            {
                newData = GameData.GothicVm.InitInstance<SvmInstance>($"SVM_{voiceId}");
            }
            catch (Exception)
            {
                // ignored
            }

            _svmDataCache[voiceId] = newData;

            return newData;
        }

        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit and don't need to be deleted during runtime.
        /// Hint: As there is a potential for multiple instances per key
        ///       (e.g., BreathBubbles, BreathBubbles_A1, BreathBubbles_A2), we need to retrieve a container holding all of them.
        /// </summary>
        [CanBeNull]
        public static SfxAdapter TryGetSfxData(string key)
        {
            var preparedKey = GetPreparedKey(key);
            if (_sfxDataCache.TryGetValue(preparedKey, out var data))
            {
                return data;
            }

            SfxAdapter newData = null;
            try
            {
                newData = new SfxAdapter(preparedKey);
            }
            catch (Exception)
            {
                // ignored
            }

            _sfxDataCache[preparedKey] = newData;

            return newData;
        }

        /// <summary>
        /// Hint: Instances only need to be initialized once in ZenKit and don't need to be deleted during runtime.
        /// </summary>
        public static ParticleEffectInstance TryGetPfxData(string key)
        {
            var preparedKey = GetPreparedKey(key);
            if (_pfxDataCache.TryGetValue(preparedKey, out var data))
            {
                return data;
            }

            ParticleEffectInstance newData = null;
            try
            {
                newData = GameData.PfxVm.InitInstance<ParticleEffectInstance>(preparedKey);
            }
            catch (Exception)
            {
                // ignored
            }

            _pfxDataCache[preparedKey] = newData;

            return newData;
        }

        private static string GetPreparedKey(string key)
        {
            var lowerKey = key.ToLower();
            var extension = Path.GetExtension(lowerKey);

            if (extension == string.Empty)
            {
                return lowerKey;
            }

            return lowerKey.Replace(extension, "");
        }

        public static void Dispose()
        {
            _itemDataCache.Clear();
            _svmDataCache.Clear();
            _sfxDataCache.Clear();
            _pfxDataCache.Clear();
        }
    }
}
