using System;
using System.Collections.Generic;
using GUZ.Core.Context;
using GUZ.Core.World;
using UnityEngine;
using ZenKit;
using ZenKit.Vobs;

namespace GUZ.Core
{
    [Serializable]
    public class MeshCullingGroup
    {
        [Range(1f, 100f)]
        public float MaximumObjectSize;

        [Range(1f, 1000f)]
        public float CullingDistance;
    }

    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GameConfiguration", order = 1)]
    public class GameConfiguration : ScriptableObject
    {
        [Header("### Controls ###")]
        public GuzContext.Controls GameControls = GuzContext.Controls.VRXrit;

        public bool EnableDeviceSimulator;

        [Header("### Developer ###")]
        public bool EnableMainMenu = true;

        public bool LoadFromSaveSlot;

        [Range(1, 15)]
        public int SaveSlotToLoad;

        public bool SpawnOldCampNpcs;

        public bool EnableNpcRoutines;

        public string SpawnAtWaypoint = string.Empty;

        public bool EnableWorldObjects = true;

        public bool EnableWorldMesh = true;

        public bool EnableBarrierVisual = true;

        [InspectorName("Experimental: Enable Decal Visuals")]
        public bool EnableDecalVisuals;

        [InspectorName("Experimental: Enable Particle Effects")]
        public bool EnableParticleEffects;

        [InspectorName("Experimental: Enable NPC Eye Blinking")]
        public bool EnableNpcEyeBlinking;

        public List<VirtualObjectType> SpawnWorldObjectTypes = new();

        public List<int> SpawnNpcInstances = new();

        [Header("### Audio ###")]
        public bool EnableGameMusic = true;

        public bool EnableGameSounds = true;

        [Header("### Lighting ###")]
        public Color SunLightColor = new(0.6901961f, 0.6901961f, 0.6901961f, 1);

        [Range(0, 1)]
        public float SunLightIntensity = 1;

        public GameTimeInterval SunUpdateInterval = GameTimeInterval.EveryGameHour;

        public Color AmbientLightColor = new(0.10196079f, 0.10196079f, 0.10196079f, 1);

        [Header("### Time ###")]
        [Range(0, 23)]
        public int StartTimeHour = 8;

        [Range(0, 59)]
        public int StartTimeMinute;

        [Range(0.5f, 1000f)]
        public float TimeSpeedMultiplier = 1;

        [Header("### WayNet ###")]
        public bool ShowFreePoints;

        public bool ShowWayPoints;

        public bool ShowWayEdges;

        [Header("### Culling ###")]
        public bool EnableSoundCulling = true;

        public bool EnableMeshCulling = true;

        public bool ShowMeshCullingGizmos = true;

        public MeshCullingGroup SmallMeshCullingGroup = new() { MaximumObjectSize = 0.2f, CullingDistance = 50 };

        public MeshCullingGroup MediumMeshCullingGroup = new() { MaximumObjectSize = 5.0f, CullingDistance = 100 };

        public MeshCullingGroup LargeMeshCullingGroup = new() { MaximumObjectSize = 100, CullingDistance = 200 };

        [Header("### Logging ###")]
        [InspectorName("ZenKit Log Level")]
        public LogLevel ZenkitLogLevel = LogLevel.Warning;

        [InspectorName("DirectMusic Log Level")]
        public DirectMusic.LogLevel DirectMusicLogLevel = DirectMusic.LogLevel.Warning;

        public bool EnableBarrierLogs;

        public bool EnableSpyLogs;
    }
}
