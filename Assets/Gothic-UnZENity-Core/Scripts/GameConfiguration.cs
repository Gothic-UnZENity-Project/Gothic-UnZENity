using System;
using System.Collections.Generic;
using GUZ.Core.Context;
using GUZ.Core.World;
using UnityEngine;
using UnityEngine.Serialization;
using ZenKit;
using ZenKit.Vobs;

namespace GUZ.Core
{
    [Serializable]
    public class MeshCullingGroup
    {
        [FormerlySerializedAs("maximumObjectSize")] [Range(1f, 100f)]
        public float MaximumObjectSize;

        [FormerlySerializedAs("cullingDistance")] [Range(1f, 1000f)]
        public float CullingDistance;
    }

    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GameConfiguration", order = 1)]
    public class GameConfiguration : ScriptableObject
    {
        [FormerlySerializedAs("enableDeviceSimulator")] [Header("### Controls ###")]
        public bool EnableDeviceSimulator;

        [FormerlySerializedAs("gameControls")] public GuzContext.Controls GameControls = GuzContext.Controls.VRXrit;

        [FormerlySerializedAs("enableMainMenu")] [Header("### Developer ###")] [SerializeField]
        public bool EnableMainMenu = true;

        [FormerlySerializedAs("loadFromSaveSlot")] [SerializeField]
        public bool LoadFromSaveSlot;

        [FormerlySerializedAs("saveSlotToLoad")] [Range(1, 15)] [SerializeField]
        public int SaveSlotToLoad;

        [FormerlySerializedAs("spawnOldCampNpcs")] [SerializeField]
        public bool SpawnOldCampNpcs;

        [FormerlySerializedAs("enableNpcRoutines")] [SerializeField]
        public bool EnableNpcRoutines;

        [FormerlySerializedAs("spawnAtWaypoint")] [SerializeField]
        public string SpawnAtWaypoint = string.Empty;

        [FormerlySerializedAs("enableWorldObjects")] [SerializeField]
        public bool EnableWorldObjects = true;

        [FormerlySerializedAs("enableWorldMesh")] [SerializeField]
        public bool EnableWorldMesh = true;

        [FormerlySerializedAs("enableBarrierVisual")] [SerializeField]
        public bool EnableBarrierVisual = true;

        [FormerlySerializedAs("enableDecalVisuals")]
        [InspectorName("Experimental: Enable Decal Visuals")]
        [SerializeField]
        public bool EnableDecalVisuals;

        [FormerlySerializedAs("enableParticleEffects")]
        [InspectorName("Experimental: Enable Particle Effects")]
        [SerializeField]
        public bool EnableParticleEffects;

        [FormerlySerializedAs("enableNpcEyeBlinking")] [InspectorName("Experimental: Enable NPC Eye Blinking")]
        public bool EnableNpcEyeBlinking;

        [FormerlySerializedAs("spawnWorldObjectTypes")] [SerializeField]
        public List<VirtualObjectType> SpawnWorldObjectTypes = new();

        [FormerlySerializedAs("spawnNpcInstances")] [SerializeField]
        public List<int> SpawnNpcInstances = new();

        [FormerlySerializedAs("enableGameMusic")] [Header("### Audio ###")] [SerializeField]
        public bool EnableGameMusic = true;

        [FormerlySerializedAs("enableGameSounds")] [SerializeField]
        public bool EnableGameSounds = true;

        [FormerlySerializedAs("sunLightColor")] [Header("### Lighting ###")] [SerializeField]
        public Color SunLightColor = new(0.6901961f, 0.6901961f, 0.6901961f, 1);

        [FormerlySerializedAs("sunLightIntensity")] [SerializeField] [Range(0, 1)]
        public float SunLightIntensity = 1;

        [FormerlySerializedAs("sunUpdateInterval")] [SerializeField]
        public GameTimeInterval SunUpdateInterval = GameTimeInterval.EveryGameHour;

        [FormerlySerializedAs("ambientLightColor")] [SerializeField]
        public Color AmbientLightColor = new(0.10196079f, 0.10196079f, 0.10196079f, 1);

        [FormerlySerializedAs("startTimeHour")] [Header("### Time ###")] [SerializeField] [Range(0, 23)]
        public int StartTimeHour = 8;

        [FormerlySerializedAs("startTimeMinute")] [SerializeField] [Range(0, 59)]
        public int StartTimeMinute;

        [FormerlySerializedAs("timeSpeedMultiplier")] [SerializeField] [Range(0.5f, 1000f)]
        public float TimeSpeedMultiplier = 1;

        [FormerlySerializedAs("showFreePoints")] [Header("### WayNet ###")] [SerializeField]
        public bool ShowFreePoints;

        [FormerlySerializedAs("showWayPoints")] [SerializeField]
        public bool ShowWayPoints;

        [FormerlySerializedAs("showWayEdges")] [SerializeField]
        public bool ShowWayEdges;

        [FormerlySerializedAs("enableSoundCulling")] [Header("### Culling ###")] [SerializeField]
        public bool EnableSoundCulling = true;

        [FormerlySerializedAs("enableMeshCulling")] [SerializeField]
        public bool EnableMeshCulling = true;

        [FormerlySerializedAs("showMeshCullingGizmos")] [SerializeField]
        public bool ShowMeshCullingGizmos = true;

        [FormerlySerializedAs("smallMeshCullingGroup")] [SerializeField]
        public MeshCullingGroup SmallMeshCullingGroup = new() { MaximumObjectSize = 0.2f, CullingDistance = 50 };

        [FormerlySerializedAs("mediumMeshCullingGroup")] [SerializeField]
        public MeshCullingGroup MediumMeshCullingGroup = new() { MaximumObjectSize = 5.0f, CullingDistance = 100 };

        [FormerlySerializedAs("largeMeshCullingGroup")] [SerializeField]
        public MeshCullingGroup LargeMeshCullingGroup = new() { MaximumObjectSize = 100, CullingDistance = 200 };

        [FormerlySerializedAs("zenkitLogLevel")]
        [Header("### Logging ###")]
        [InspectorName("ZenKit Log Level")]
        [SerializeField]
        public LogLevel ZenkitLogLevel = LogLevel.Warning;

        [FormerlySerializedAs("directMusicLogLevel")] [InspectorName("DirectMusic Log Level")] [SerializeField]
        public DirectMusic.LogLevel DirectMusicLogLevel = DirectMusic.LogLevel.Warning;

        [FormerlySerializedAs("enableBarrierLogs")] [SerializeField]
        public bool EnableBarrierLogs;

        [FormerlySerializedAs("enableSpyLogs")] [SerializeField]
        public bool EnableSpyLogs;
    }
}
