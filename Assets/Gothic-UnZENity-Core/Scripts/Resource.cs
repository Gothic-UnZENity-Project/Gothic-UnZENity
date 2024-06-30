using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ZenKit;

namespace GUZ.Core
{
    /// <summary>
    /// <b>Represents a cached resource of a given type <see cref="T"/>.</b>
    /// <p>
    /// It works by getting a callback function, which
    /// can load a given asset by a name. If loading an asset using the loader succeeds, it is saved into an internal
    /// dictionary by name and returned immediately on subsequent loads of an asset with the same name.
    /// </p>
    /// <p>
    /// Caching is case-sensitive and errors during load are handled gracefully by returning <c>null</c>.
    /// </p>
    /// </summary>
    /// <typeparam name="T">The type of the resource, e.g. <see cref="Model"/></typeparam>
    public class Resource<T>
    {
        private readonly Dictionary<string, T> _cache = new();
        private readonly Func<string, T> _loader;

        /// <summary>
        /// Constructs a new resource given a loader.
        /// </summary>
        /// <param name="loader">
        /// A function which, when called, attempts to load an asset of type <see cref="T"/> with a given name. This
        /// function is only called once for each unique asset name. May throw.
        /// </param>
        public Resource(Func<string, T> loader)
        {
            _loader = loader;
        }

        /// <summary>
        /// Tries to load an asset of type <see cref="T"/> with the given name (<see cref="key"/>). If the asset has
        /// already been loaded before, a cached version of the asset is returned, saving a call to <see cref="_loader"/>,
        /// otherwise, the <see cref="_loader"/> is called and its result cached and returned.
        /// </summary>
        /// <param name="key">The name of the asset to load by calling the <see cref="_loader"/> function</param>
        /// <param name="value">The asset to be returned (out)</param>
        /// <returns><c>true</c> if loading the asset succeeded and <c>false</c> if it failed</returns>
        public bool TryLoad([NotNull] string key, out T value)
        {
            if (_cache.TryGetValue(key, out value))
            {
                return true;
            }

            try
            {
                value = _loader(key);
                _cache[key] = value;
                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            _cache[key] = default;
            return false;
        }
    }
}
