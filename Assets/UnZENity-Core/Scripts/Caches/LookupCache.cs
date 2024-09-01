using System.Collections.Generic;
using GUZ.Core.Properties;
using TMPro;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Caches
{
    /// <summary>
    /// Contains lookup caches for GameObjects for faster use.
    /// </summary>
    public static class LookupCache
    {
        /// <summary>
        /// [symbolIndex] = {zkInstance => NpcInstance from ZenKit, properties => Properties component (MonoBehaviour)}
        /// Hints:
        ///     * Includes NPCs and Hero (Easier for lookups like "what is the nearest enemy in range".)
        ///     * Doesn't include all the Monsters Properties as they have same symbolIndex for multiple GOs. But it's not needed to look them up.
        ///     * During loading time, we have no option to understand what is an NPC and what a Monster. We therefore have the first entry of each monster Id in here.
        /// </summary>
        public static readonly Dictionary<int, (NpcInstance instance, NpcProperties properties)> NpcCache = new();

        /// <summary>
        /// Already created AnimationData (Clips + RootMotions) can be reused.
        /// </summary>
        public static readonly Dictionary<string, AnimationClip> AnimationClipCache = new();

        /// <summary>
        /// This dictionary caches the sprite assets for fonts.
        /// </summary>
        public static Dictionary<string, TMP_SpriteAsset> FontCache = new();

        public static void Init()
        {
            GlobalEventDispatcher.GeneralSceneUnloaded.AddListener(delegate
            {
                NpcCache.Clear();
            });
        }

        public static void Dispose()
        {
            NpcCache.Clear();
            AnimationClipCache.Clear();
            FontCache.Clear();
        }
    }
}
