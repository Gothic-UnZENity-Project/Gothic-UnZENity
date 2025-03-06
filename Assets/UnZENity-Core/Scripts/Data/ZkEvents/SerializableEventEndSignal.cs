using System;
using JetBrains.Annotations;

namespace GUZ.Core.Data.ZkEvents
{
    /// <summary>
    /// UnityEngine.JsonUtility doesn't serialize CachedEventTag. We therefore use this class to JSON-ify the data.
    /// </summary>
    [Obsolete("Use via AbstractAnimation timer instead. As we use BlendIn/BlendOut, this Message is never been called.")]
    public class SerializableEventEndSignal
    {
        public string NextAnimation;

        public SerializableEventEndSignal([NotNull] string nextAnimation)
        {
            NextAnimation = nextAnimation;
        }
    }
}
