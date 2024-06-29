using System.Collections.Generic;
using GUZ.Core.Properties;
using TMPro;
using UnityEngine;

namespace GUZ.Core.Caches
{
    /// <summary>
    /// Contains lookup caches for GameObjects for faster use.
    /// </summary>
    public static class LookupCache
    {
        /// <summary>
        /// [symbolIndex] = Properties-Component
        /// Hint: Includes NPCs and Hero (Easier for lookups like "what is nearest enemy in range".)
        /// </summary>
        public static readonly Dictionary<int, NpcProperties> NpcCache = new();

        /// <summary>
        /// Already created AnimationData (Clips + RootMotions) can be reused.
        /// </summary>
        public static readonly Dictionary<string, AnimationClip> AnimationClipCache = new();

        /// <summary>
        /// This dictionary caches the sprite assets for fonts.
        /// </summary>
        public static Dictionary<string, TMP_SpriteAsset> FontCache = new();

        /// <summary>
        /// VobSounds and VobSoundsDayTime GOs.
        /// </summary>
        public static List<GameObject> VobSoundsAndDayTime = new();

        public static void Init()
        {
            GlobalEventDispatcher.GeneralSceneUnloaded.AddListener(delegate
            {
                NpcCache.Clear();
                VobSoundsAndDayTime.Clear();
            });
        }

        public static void Dispose()
        {
            NpcCache.Clear();
            AnimationClipCache.Clear();
            FontCache.Clear();
            VobSoundsAndDayTime.Clear();
        }
    }
}
