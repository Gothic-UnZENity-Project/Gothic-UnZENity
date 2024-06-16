using System;
using System.Collections.Generic;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using Unity.Collections;
using UnityEngine;

namespace GUZ.Core
{
	public class GameManager : MonoBehaviour, CoroutineManager
	{
		public GameConfiguration config;

		private VobMeshCullingManager _meshCullingManager;
		private VobSoundCullingManager _soundCullingManager;
		private BarrierManager _barrierManager;
		private SkyManager _skyVisualManager;
		private StationaryLightsManager _stationaryLightsManager;
		private XRDeviceSimulatorManager _xrSimulatorManager;


		private void Awake()
		{
			_meshCullingManager = new VobMeshCullingManager(config, this);
			_soundCullingManager = new VobSoundCullingManager(config);
			_barrierManager = new BarrierManager();
			_skyVisualManager = new SkyManager(config);
			_stationaryLightsManager = new StationaryLightsManager();
			_xrSimulatorManager = new XRDeviceSimulatorManager(config);

			_meshCullingManager.Init();
			_soundCullingManager.Init();
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