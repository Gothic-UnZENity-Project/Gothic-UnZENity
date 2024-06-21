using System;
using System.Collections.Generic;
using GUZ.Core.Context;
using GUZ.Core.World;
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
		[Header("### Controls ###")] public bool enableDeviceSimulator = false;
        public GUZContext.Controls gameControls = GUZContext.Controls.VR_XRIT;

		[Header("### Developer ###")] [SerializeField]
		public bool enableMainMenu = true;

		[SerializeField] public bool spawnOldCampNpcs = false;
		[SerializeField] public bool enableNpcRoutines = false;

		[SerializeField] public string spawnAtWaypoint = string.Empty;

		[SerializeField] public bool enableWorldObjects = true;
		[SerializeField] public bool enableWorldMesh = true;
		[SerializeField] public bool enableBarrierVisual = true;
		[SerializeField] public bool enableSkyVisual = true;
		
		[InspectorName("Experimental: Enable Decal Visuals")]
		[SerializeField] public bool enableDecalVisuals = true;
		
		[InspectorName("Experimental: Enable Particle Effects")]
		[SerializeField] public bool enableParticleEffects = false;

		[InspectorName("Experimental: Enable NPC Eye Blinking")]
		public bool enableNpcEyeBlinking = false;
		
		[SerializeField] public List<ZenKit.Vobs.VirtualObjectType> spawnWorldObjectTypes = new();
		[SerializeField] public List<int> spawnNpcInstances = new();

		[Header("### Audio ###")] [SerializeField]
		public bool enableGameMusic = true;

		[SerializeField] public bool enableGameSounds = true;

		[Header("### Lighting ###")] 
		[SerializeField] public Color sunLightColor = new(r: 0.6901961f, g: 0.6901961f, b: 0.6901961f, a: 1);

		[SerializeField] [Range(0, 1)] public float sunLightIntensity = 1;

		[SerializeField] public GameTimeInterval sunUpdateInterval = GameTimeInterval.EveryGameHour;

		[SerializeField] public Color ambientLightColor = new(r: 0.10196079f, g: 0.10196079f, b: 0.10196079f, a: 1);

		[Header("### Time ###")] [SerializeField] [Range(0, 23)]
		public int startTimeHour = 8;

		[SerializeField] [Range(0, 59)] public int startTimeMinute = 0;

		[SerializeField] [Range(0.5f, 1000f)] public float timeSpeedMultiplier = 1;

		[Header("### WayNet ###")] [SerializeField]
		public bool showFreePoints = false;

		[SerializeField] public bool showWayPoints = false;

		[SerializeField] public bool showWayEdges = false;

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

		[Header("### Logging ###")] [SerializeField] [InspectorName("ZenKit Log Level")]
		public ZenKit.LogLevel zenkitLogLevel = ZenKit.LogLevel.Warning;

		[SerializeField] [InspectorName("DirectMusic Log Level")]
		public DirectMusic.LogLevel directMusicLogLevel = DirectMusic.LogLevel.Warning;

		[SerializeField] public bool enableBarrierLogs = false;
		[SerializeField] public bool enableSpyLogs = false;
	}
}