using System;
using System.Collections.Generic;
using GUZ.Core.Context;
using GUZ.Core.World;
using MyBox;
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
#region Controls
        [Foldout("Controls", true)]
        public GuzContext.Controls GameControls = GuzContext.Controls.VRXrit;

        public bool EnableDeviceSimulator;
#endregion


#region Developer
        [Foldout("Developer", true)]
        public bool EnableMainMenu = true;

        public bool LoadFromSaveSlot;

        [Range(1, 15)]
        public int SaveSlotToLoad;
        private bool SaveSlotPredicate() => !EnableMainMenu && LoadFromSaveSlot;

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
#endregion


#region Audio
        [Foldout("Audio", true)]
        public bool EnableGameMusic = true;

        public bool EnableGameSounds = true;
#endregion


#region Lighting
        [Foldout("Lighting", true)]
        public Color SunLightColor = new(0.6901961f, 0.6901961f, 0.6901961f, 1);

        [Range(0, 1)]
        public float SunLightIntensity = 1;

        public GameTimeInterval SunUpdateInterval = GameTimeInterval.EveryGameHour;

        public Color AmbientLightColor = new(0.10196079f, 0.10196079f, 0.10196079f, 1);
#endregion


#region Time
        [Foldout("Time", true)]
        [Range(0, 23)]
        public int StartTimeHour = 8;

        [Range(0, 59)]
        public int StartTimeMinute;

        [Range(0.5f, 1000f)]
        public float TimeSpeedMultiplier = 1;
#endregion


#region WayNet
        [Foldout("WayNet", true)]
        public bool ShowFreePoints;

        public bool ShowWayPoints;

        public bool ShowWayEdges;
#endregion


#region Culling
        [Foldout("Culling", true)]
        [Separator("Misc")]
        public bool EnableSoundCulling = true;

        public bool EnableMeshCulling = true;

        public bool ShowMeshCullingGizmos = true;

        [Separator("VOB Culling")]
        public MeshCullingGroup SmallMeshCullingGroup = new() { MaximumObjectSize = 0.2f, CullingDistance = 50 };

        public MeshCullingGroup MediumMeshCullingGroup = new() { MaximumObjectSize = 5.0f, CullingDistance = 100 };

        public MeshCullingGroup LargeMeshCullingGroup = new() { MaximumObjectSize = 100, CullingDistance = 200 };
#endregion


#region Logging
        [Foldout("Logging", true)]
        [InspectorName("ZenKit Log Level")]
        public LogLevel ZenkitLogLevel = LogLevel.Warning;

        [Tooltip("Enable Daedalus logs which are called ZSpyLogs inside .d scripts.")]
        public bool EnableSpyLogs;

        [InspectorName("DirectMusic Log Level")]
        public DirectMusic.LogLevel DirectMusicLogLevel = DirectMusic.LogLevel.Warning;

        public bool EnableBarrierLogs;
#endregion
    }
}
