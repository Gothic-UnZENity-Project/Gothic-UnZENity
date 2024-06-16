using GUZ.Core.Manager.Culling;
using UnityEngine;

namespace GUZ.Core
{
	public class GameManager : MonoBehaviour, CoroutineManager
	{
		public GameConfiguration config;

		private VobMeshCullingManager _meshCullingManager;
		private VobSoundCullingManager _soundCullingManager;
		

		private void Awake()
		{
			_meshCullingManager = new VobMeshCullingManager(config, this);
			_soundCullingManager = new VobSoundCullingManager(config);
			_meshCullingManager.Init();
			_soundCullingManager.Init();
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
