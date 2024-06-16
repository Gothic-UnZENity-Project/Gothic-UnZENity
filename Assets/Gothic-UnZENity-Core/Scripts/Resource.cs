using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit;

namespace GUZ.Core
{
	public class Resource<T>
	{
		private readonly Dictionary<string, T> cache = new();
		private readonly Func<string, T> loader;

		public Resource(Func<string, T> loader)
		{
			this.loader = loader;
		}

		public bool TryLoad([NotNull] string key, out T value)
		{
			if (cache.TryGetValue(key, out value))
			{
				return true;
			}

			try
			{
				value = loader(key);
				cache[key] = value;
				return true;
			}
			catch (Exception)
			{
				// ignored
			}

			cache[key] = default;
			return false;
		}
	}
}