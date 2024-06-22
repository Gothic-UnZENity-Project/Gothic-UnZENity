using System;
using GUZ.Core;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.World;

namespace GUZ.Core
{
	public interface IGlobalDataProvider
	{
		public GameConfiguration Config { get; }
		public GameSettings Settings { get; }
		public SkyManager Sky { get; }
		public GameTime Time { get; }
		public RoutineManager Routines { get; }
		public TextureManager Textures { get; }
		public GUZSceneManager Scene { get; }
		public FontManager Font { get; }
		public StationaryLightsManager Lights { get; }
		public VobMeshCullingManager MeshCulling { get; }
		public VobSoundCullingManager SoundCulling { get; }
	}

	public static class GameGlobals
	{
		public static IGlobalDataProvider Instance;
		
		[Obsolete("Don't use globals.")] public static GameConfiguration Config => Instance.Config;
		[Obsolete("Don't use globals.")] public static GameSettings Settings => Instance.Settings;
		[Obsolete("Don't use globals.")] public static SkyManager Sky => Instance.Sky;
		[Obsolete("Don't use globals.")] public static GameTime Time => Instance.Time;
		[Obsolete("Don't use globals.")] public static RoutineManager Routines => Instance.Routines;
		[Obsolete("Don't use globals.")] public static TextureManager Textures => Instance.Textures;
		[Obsolete("Don't use globals.")] public static GUZSceneManager Scene => Instance.Scene;
		[Obsolete("Don't use globals.")] public static FontManager Font => Instance.Font;
		[Obsolete("Don't use globals.")] public static StationaryLightsManager Lights => Instance.Lights;
		[Obsolete("Don't use globals.")] public static VobMeshCullingManager MeshCulling => Instance.MeshCulling;
		[Obsolete("Don't use globals.")] public static VobSoundCullingManager SoundCulling => Instance.SoundCulling;
	}
}