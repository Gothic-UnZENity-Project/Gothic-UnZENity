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
		[Header("### Lighting ###")] [SerializeField]
		public Quaternion sunLightDirection = new(x: 0.45451945f, y: 0.54167527f, z: -0.45451945f, w: 0.54167527f);

		[SerializeField] public Color sunLightColor = new(r: 0.6901961f, g: 0.6901961f, b: 0.6901961f, a: 1);

		[SerializeField] [Range(0, 1)] public float sunLightIntensity = 1;

		[SerializeField] public Color ambientLightColor = new(r: 0.10196079f, g: 0.10196079f, b: 0.10196079f, a: 1);

		[Header("### Culling ###")] [SerializeField]
		public bool enableSoundCulling = true;

		[SerializeField] public bool enableMeshCulling = true;

		[SerializeField] public bool showMeshCullingGizmos = true;

		[SerializeField]
		public MeshCullingGroup smallMeshCullingGroup = new() { maximumObjectSize = 0.2f, cullingDistance = 50 };

		[SerializeField]
		public MeshCullingGroup mediumMeshCullingGroup = new() { maximumObjectSize = 5.0f, cullingDistance = 100 };

		[SerializeField]
		public MeshCullingGroup largeMeshCullingGroup = new() { maximumObjectSize = 100, cullingDistance = 200 };
	}
}