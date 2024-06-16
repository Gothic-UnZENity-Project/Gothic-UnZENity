using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.World;
using UnityEngine;

namespace GUZ.Core
{
	public class GameManager : MonoBehaviour, CoroutineManager
	{
		public GameConfiguration config;

		private SettingsManager _gameSettingsManager;
		private VobMeshCullingManager _meshCullingManager;
		private VobSoundCullingManager _soundCullingManager;
		private BarrierManager _barrierManager;
		private SkyManager _skyVisualManager;
		private StationaryLightsManager _stationaryLightsManager;
		private XRDeviceSimulatorManager _xrSimulatorManager;
		private GameTime _gameTimeManager;


		private void Awake()
		{
			_gameSettingsManager = new SettingsManager();
			_meshCullingManager = new VobMeshCullingManager(config, this);
			_soundCullingManager = new VobSoundCullingManager(config);
			_barrierManager = new BarrierManager();
			_skyVisualManager = new SkyManager(config);
			_stationaryLightsManager = new StationaryLightsManager();
			_xrSimulatorManager = new XRDeviceSimulatorManager(config);
			_gameTimeManager = new GameTime(config, this);

			_gameSettingsManager.Init();
			_meshCullingManager.Init();
			_soundCullingManager.Init();
			_gameTimeManager.Init();
		}

		private void Start()
		{
			_skyVisualManager.Init();
			_xrSimulatorManager.Init();
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