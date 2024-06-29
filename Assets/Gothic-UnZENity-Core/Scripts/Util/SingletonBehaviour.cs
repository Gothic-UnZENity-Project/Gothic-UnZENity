using UnityEngine;

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
                Debug.LogWarning("An instance of this singleton (" + Instance.name + ") already exists. Destroying " +
                                 gameObject);
                Destroy(gameObject);
            }
            else
            {
                Instance = (T)this;
            }
        }
    }
}
