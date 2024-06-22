using System;
using GUZ.Core;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Settings;

namespace GUZ.Core
{
	public interface GlobalDataProvider
	{
		public static GlobalDataProvider Instance;

		[Obsolete("Don't use globals.")] public GameConfiguration Config { get; }
		[Obsolete("Don't use globals.")] public GameSettings Settings { get; }
		[Obsolete("Don't use globals.")] public SkyManager Sky { get; }
	}
}