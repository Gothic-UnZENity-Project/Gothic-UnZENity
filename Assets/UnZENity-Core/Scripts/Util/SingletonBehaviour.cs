using GUZ.Core.Core.Logging;
using UnityEngine;
using Logger = GUZ.Core.Core.Logging.Logger;

namespace GUZ.Core.Util
{
    public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        protected static T Instance;

        public static bool Created => Instance != null;

        /// <summary>
        /// Always returns the first created instance.
        /// </summary>
        public static T I => Instance;

        protected virtual void Awake()
        {
            if (Created && Instance != this)
            {
                Logger.LogWarningEditor($"An instance of this singleton ({Instance.name} already exists. Destroying {gameObject}",
                    LogCat.Misc);
                Destroy(gameObject);
            }
            else
            {
                Instance = (T)this;
            }
        }
    }
}
