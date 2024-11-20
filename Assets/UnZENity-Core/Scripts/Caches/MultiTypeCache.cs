using System.Collections.Generic;
using GUZ.Core.Data.Container;
using TMPro;
using UnityEngine;

namespace GUZ.Core.Caches
{
    /// <summary>
    /// Contains lookup caches for GameObjects, AudioClips and other Unity objects for faster use.
    /// Caching of ZenKit data is done inside ResourceLoader.cs
    /// </summary>
    public static class MultiTypeCache
    {
        /// <summary>
        /// Hints:
        ///     * Includes NPCs and Hero (Easier for lookups like "what is the nearest enemy in range".)
        ///     * Also includes all monsters
        ///     * We need to ensure that any time an NpcInstance.UserData contains an NpcData object, that it is stored here.
        ///       Otherwise, UserData's WeakReference pointer gets cleared.
        /// </summary>
        public static readonly List<NpcContainer> NpcCache = new();

        /// <summary>
        /// Already created AnimationData (Clips + RootMotions) can be reused.
        /// </summary>
        public static readonly Dictionary<string, AnimationClip> AnimationClipCache = new();

        /// <summary>
        /// This dictionary caches the sprite assets for fonts.
        /// </summary>
        public static Dictionary<string, TMP_SpriteAsset> FontCache = new();
        
        /// <summary>
        /// Caching all types of Meshes (World?, Vob, Npc) to optimize memory usage of meshes if duplicated
        /// (e.g. multiple VOBs sharing the same mesh)
        /// </summary>
        public static Dictionary<string, Mesh> Meshes = new();
        
        public static Dictionary<string, AudioClip> AudioClips = new();

        
        public static void Init()
        {
            GlobalEventDispatcher.LoadingSceneLoaded.AddListener(delegate
            {
                NpcCache.Clear();
            });
        }

        public static void Dispose()
        {
            NpcCache.Clear();
            AnimationClipCache.Clear();
            FontCache.Clear();
            Meshes.Clear();
            AudioClips.Clear();
        }
    }
}
