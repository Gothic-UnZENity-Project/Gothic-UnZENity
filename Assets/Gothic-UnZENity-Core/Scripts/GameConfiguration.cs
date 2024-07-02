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

    [CreateAssetMenu(fileName = "NewGameConfiguration", menuName = "UnZENity/ScriptableObjects/GameConfiguration", order = 1)]
    public class GameConfiguration : ScriptableObject
    {

#region Controls
        [Foldout("Controls", true)]
        public GuzContext.Controls GameControls = GuzContext.Controls.HVR;
        public bool EnableDeviceSimulator;
#endregion


#region Logging
        [Foldout("Logging", true)]
        [OverrideLabel("ZenKit Log Level")]
        public LogLevel ZenKitLogLevel = LogLevel.Warning;

        [Tooltip("Enable Daedalus logs inside .d scripts.")]
        public bool EnableZSpyLogs;

        [OverrideLabel("DirectMusic Log Level")]
        public DirectMusic.LogLevel DirectMusicLogLevel = DirectMusic.LogLevel.Warning;
        public bool EnableBarrierLogs;
#endregion


#region Menu and Loading
        [Foldout("Menu and Loading", true)]
        public bool EnableMainMenu = true;
        public bool LoadFromSaveSlot;

        [Range(1, 15)]
        public int SaveSlotToLoad;
        private bool SaveSlotPredicate() => !EnableMainMenu && LoadFromSaveSlot;
#endregion


#region VOBs
        [Foldout("VOBs", true)]
        [Tooltip("Enable World objects.")]
        public bool EnableVOBs = true;

        [Tooltip("Spawn only specific VOBs by naming their types in here.")]
        public List<VirtualObjectType> SpawnVOBTypes = new();
#endregion


#region NPCs
        [Foldout("NPCs", true)]
        public bool SpawnOldCampNpcs;
        public bool EnableNpcRoutines;

        [Tooltip("Spawn only specific NPCs by naming their IDs in here.")]
        public List<int> SpawnNpcInstances = new();
#endregion


#region WayNet
        [Foldout("WayNet", true)]
        [OverrideLabel("Show Free Point Meshes")]
        public bool ShowFreePoints;

        [OverrideLabel("Show Way Point Meshes")]
        public bool ShowWayPoints;

        [OverrideLabel("Show Way Point Edge Meshes")]
        public bool ShowWayEdges;

        [Tooltip("Covers Free Points and Way Points.")]
        public string SpawnAtWaypoint = string.Empty;
#endregion


#region Audio
        [Foldout("Audio", true)]
        public bool EnableGameMusic = true;
        public bool EnableGameSounds = true;
#endregion


#region Lighting
        [Foldout("Lighting", true)]
        public Color SunLightColor = new(0.69f, 0.69f, 0.69f, 1);

        [Range(0, 1)]
        public float SunLightIntensity = 1;
        public GameTimeInterval SunUpdateInterval = GameTimeInterval.EveryGameMinute;
        public Color AmbientLightColor = new(0.1f, 0.1f, 0.1f, 1);
#endregion


#region Time
        [Foldout("Time", true)]
        [Range(0, 23)]
        public int StartTimeHour = 8;

        [Range(0, 59)]
        public int StartTimeMinute;

        [Range(0.5f, 1000f)]
        [Tooltip("Speeds up the in game time.")]
        public float TimeSpeedMultiplier = 1;
#endregion


#region Culling
        [Foldout("Culling", true)]
        [Separator("VOBs")]
        public MeshCullingGroup SmallMeshCullingGroup = new() { MaximumObjectSize = 0.2f, CullingDistance = 50 };
        public MeshCullingGroup MediumMeshCullingGroup = new() { MaximumObjectSize = 5.0f, CullingDistance = 100 };
        public MeshCullingGroup LargeMeshCullingGroup = new() { MaximumObjectSize = 100, CullingDistance = 200 };

        [Separator("Misc")]
        public bool EnableSoundCulling = true;
        public bool EnableMeshCulling = true;
        public bool ShowMeshCullingGizmos;
#endregion


#region Misc
        [Foldout("Misc", true)]
        [Separator("Meshes/Visuals")]
        public bool EnableWorldMesh = true;
        public bool EnableBarrierVisual = true;
#endregion


#region Experimental - Not production ready
        [Foldout("Experimental - Not production ready", true)]
        public bool EnableDecalVisuals;
        public bool EnableParticleEffects;
        public bool EnableNpcEyeBlinking;
#endregion

    }
}
