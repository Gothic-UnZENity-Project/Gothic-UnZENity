using System;
using GUZ.Core.Manager.Culling;
using Unity.VisualScripting;
using UnityEngine;

namespace GUZ.Core
{
	public class GameManager : MonoBehaviour, CoroutineManager
	{
		public GameConfiguration config;

		private VobMeshCullingManager _meshCullingManager;
		

		private void Awake()
		{
			_meshCullingManager = new VobMeshCullingManager(config, this);
			_meshCullingManager.Init();
		}

		private void OnDrawGizmos()
		{
			_meshCullingManager.OnDrawGizmos();
		}

		public void OnDestroy()
		{
			_meshCullingManager.Destroy();
		}
	}
}
