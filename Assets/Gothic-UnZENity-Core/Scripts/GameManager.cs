using GUZ.Core.Caches;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.World;
using GVR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core
{
	internal static class Logging
	{
		public static void OnZenKitLogMessage(ZenKit.LogLevel level, string name, string message)
		{
			// Using the fastest string concatenation as we might have a lot of logs here.
			var messageString = string.Concat("level=", level, ", name=", name, ", message=", message);

			switch (level)
			{
				case ZenKit.LogLevel.Error:
					Debug.LogError(messageString);
					break;
				case ZenKit.LogLevel.Warning:
					Debug.LogWarning(messageString);
					break;
				case ZenKit.LogLevel.Info:
				case ZenKit.LogLevel.Debug:
				case ZenKit.LogLevel.Trace:
					Debug.Log(messageString);
					break;
			}
		}

		public static void OnDirectMusicLogMessage(DirectMusic.LogLevel level, string message)
		{
			// Using the fastest string concatenation as we might have a lot of logs here.
			var messageString = string.Concat("level=", level, ", message=", message);

			switch (level)
			{
				case DirectMusic.LogLevel.Error:
					Debug.LogError(messageString);
					break;
				case DirectMusic.LogLevel.Warning:
					Debug.LogWarning(messageString);
					break;
				case DirectMusic.LogLevel.Info:
				case DirectMusic.LogLevel.Debug:
				case DirectMusic.LogLevel.Trace:
					Debug.Log(messageString);
					break;
			}
		}
	}

	public class GameManager : MonoBehaviour, CoroutineManager
	{
		public GameConfiguration config;
		public GameObject xrInteractionManager;
		public GameObject invalidInstallationPathMessage;

		private SettingsManager _gameSettingsManager;
		private VobMeshCullingManager _meshCullingManager;
		private VobSoundCullingManager _soundCullingManager;
		private BarrierManager _barrierManager;
		private SkyManager _skyVisualManager;
		private StationaryLightsManager _stationaryLightsManager;
		private XRDeviceSimulatorManager _xrSimulatorManager;
		private GameTime _gameTimeManager;
		private MusicManager _gameMusicManager;
		private GUZSceneManager _gameSceneManager;
		private LoadingManager _gameLoadingManager;

		private bool _isInitialised = false;

		// ReSharper disable Unity.PerformanceAnalysis
		private void Load()
		{
			// If the Gothic installation directory is not set, show an error message and exit.
			if (!_gameSettingsManager.CheckIfGothic1InstallationExists())
			{
				invalidInstallationPathMessage.SetActive(true);
				return;
			}

			// Otherwise, continue loading Gothic.
            ResourceLoader.Init(SettingsManager.GameSettings.GothicIPath);
	
            _gameMusicManager.Init();
            
			GUZBootstrapper.BootGothicUnZENity(SettingsManager.GameSettings.GothicIPath);
			_gameSceneManager.LoadStartupScenes();

			if (config.enableBarrierVisual)
			{
				_barrierManager.CreateBarrier();
			}
		}

		private void Awake()
		{
			LookupCache.Init();

			_gameLoadingManager = new LoadingManager();
			_gameSettingsManager = new SettingsManager();
			_meshCullingManager = new VobMeshCullingManager(config, this);
			_soundCullingManager = new VobSoundCullingManager(config);
			_barrierManager = new BarrierManager(config);
			_stationaryLightsManager = new StationaryLightsManager();
			_xrSimulatorManager = new XRDeviceSimulatorManager(config);
			_gameTimeManager = new GameTime(config, this);
			_skyVisualManager = new SkyManager(config, _gameTimeManager);
			_gameMusicManager = new MusicManager(config);
			_gameSceneManager = new GUZSceneManager(config, _gameLoadingManager, xrInteractionManager);
		}

		private void Start()
		{
			ZenKit.Logger.Set(config.zenkitLogLevel, Logging.OnZenKitLogMessage);
			DirectMusic.Logger.Set(config.directMusicLogLevel, Logging.OnDirectMusicLogMessage);

			_gameLoadingManager.Init();
			_gameSettingsManager.Init();
			_meshCullingManager.Init();
			_soundCullingManager.Init();
			_gameTimeManager.Init();
			_skyVisualManager.Init();
			_xrSimulatorManager.Init();
			_gameSceneManager.Init();

			// Just in case we forgot to disable it in scene view. ;-)
			invalidInstallationPathMessage.SetActive(false);
		}

		private void Update()
		{
			_gameSceneManager.Update();

			if (_isInitialised)
			{
				return;
			}

			_isInitialised = true;
			Load();
		}

		private void FixedUpdate()
		{
			_barrierManager.FixedUpdate();
		}

		private void LateUpdate()
		{
			_stationaryLightsManager.LateUpdate();
		}

		private void OnValidate()
		{
			_skyVisualManager?.OnValidate();
		}

		private void OnDrawGizmos()
		{
			_meshCullingManager?.OnDrawGizmos();
		}

		public void OnDestroy()
		{
			_meshCullingManager.Destroy();
			_soundCullingManager.Destroy();
		}
	}
}