using System;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using UnityEngine;

namespace GUZ.Core
{
	public class GameManager : MonoBehaviour, CoroutineManager
	{
		public GameConfiguration config;

		private VobMeshCullingManager _meshCullingManager;
		private VobSoundCullingManager _soundCullingManager;
		private BarrierManager _barrierManager;
		

		private void Awake()
		{
			_meshCullingManager = new VobMeshCullingManager(config, this);
			_soundCullingManager = new VobSoundCullingManager(config);
			_barrierManager = new BarrierManager();
			_meshCullingManager.Init();
			_soundCullingManager.Init();
		}

		private void FixedUpdate()
		{
			_barrierManager.FixedUpdate();
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
