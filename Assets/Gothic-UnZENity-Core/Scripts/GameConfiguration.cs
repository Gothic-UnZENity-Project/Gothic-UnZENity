using System;
using UnityEngine;

namespace GUZ.Core
{
	[Serializable]
	public class MeshCullingGroup
	{
		[Range(1f, 100f)] public float maximumObjectSize;
		[Range(1f, 1000f)] public float cullingDistance;
	}

	[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GameConfiguration", order = 1)]
	public class GameConfiguration : ScriptableObject
	{
		[Header("### Culling ###")] [SerializeField]
		public bool enableMeshCulling = true;

		[SerializeField] public bool showMeshCullingGizmos = true;

		[SerializeField]
		public MeshCullingGroup smallMeshCullingGroup = new() { maximumObjectSize = 0.2f, cullingDistance = 50 };

		[SerializeField]
		public MeshCullingGroup mediumMeshCullingGroup = new() { maximumObjectSize = 5.0f, cullingDistance = 100 };

		[SerializeField]
		public MeshCullingGroup largeMeshCullingGroup = new() { maximumObjectSize = 100, cullingDistance = 200 };
	}
}