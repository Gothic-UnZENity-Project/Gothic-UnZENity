using UnityEngine;

namespace GUZ.Core.Globals
{
    public static class Constants
    {
        public static readonly Material LoadingMaterial; // Used for Vobs and World before applying TextureArray.

        // Unity shaders
        public static readonly Shader ShaderUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        public static readonly Shader ShaderUnlitParticles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        public static readonly Shader ShaderTMPSprite = Shader.Find("TextMeshPro/Sprite");
        public static readonly Shader ShaderDecal = Shader.Find("Shader Graphs/Decal");
        public static readonly Shader ShaderStandard = Shader.Find("Standard");

        // Custom GUZ shaders
        // For textures like NPCs, _not_ the grouped texture array.
        public static readonly Shader ShaderSingleMeshLit = Shader.Find("Lit/SingleMesh");

        public static readonly Shader ShaderWorldLit = Shader.Find("Lit/World");
        public static readonly Shader ShaderLitAlphaToCoverage = Shader.Find("Lit/AlphaToCoverage");
        public static readonly Shader ShaderWater = Shader.Find("Lit/Water");
        public static readonly Shader ShaderBarrier = Shader.Find("Unlit/Barrier");
        public static readonly Shader ShaderThunder = Shader.Find("Unlit/ThunderShader");

        // Shader properties
        public static readonly float ShaderPropertyFocusBrightness = 10f; // Object is in focus of player. Brighten up the color by this value.

        public const string SceneBootstrap = "Bootstrap";
        public const string SceneGeneral = "General";
        public const string SceneMainMenu = "MainMenu";
        public const string SceneLoading = "Loading";
        public const string SceneLab = "Lab";


        /*
         * ### Layers
         */

        // Unity's built-in layers
        public static LayerMask DefaultLayer = LayerMask.NameToLayer("Default");
        public static LayerMask TransparentFXLayer = LayerMask.NameToLayer("TransparentFX");
        // solves some weird interactions between the teleport raycast and collider (musicZone/worldTriggerChange)
        public static LayerMask IgnoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        public static LayerMask WaterLayer = LayerMask.NameToLayer("Water");
        public static LayerMask UILayer = LayerMask.NameToLayer("UI");

        // HVR Layers (could also be reused and implemented for XRIT if needed)
        public static LayerMask PlayerLayer = LayerMask.NameToLayer("Player"); // Layer 8 (suggested by HVR)
        public static LayerMask DynamicPoseLayer = LayerMask.NameToLayer("DynamicPose"); // Layer 9 (suggested by HVR)
        public static LayerMask GrabbableLayer = LayerMask.NameToLayer("Grabbable"); // Layer 20 (suggested by HVR)
        public static LayerMask HandLayer = LayerMask.NameToLayer("Hand"); // Layer 21 (suggested by HVR)

        // Custom layers
        public static LayerMask VobRotatableLayer = LayerMask.NameToLayer("VobRotatable"); // No collision with world (e.g. for chest lid and door)
        public static LayerMask VobItemNoCollision = LayerMask.NameToLayer("VobItemNoCollision"); // No collision with world (e.g. while item is in our hands)

        // Tags
        public const string ClimbableTag = "Climbable";
        public const string SpotTag = "PxVob_zCVobSpot";
        public const string PlayerTag = "Player";

        public static int MeshPerFrame { get; } = 10;
        public static int VobsPerFrame { get; } = 75;
        public static int NpcsPerFrame { get; } = 75;

        //Collection of PlayerPref entries for VR settings
        public const string PlayerPrefDirectionMode = "DirectionMode";
        public const string PlayerPrefRotationType = "RotationType";
        public const string PlayerPrefSnapRotationAmount = "SnapRotationAmount";
        public const string PlayerPrefSmoothRotationSpeed = "SmoothRotationSpeed";
        public const string PlayerPrefMusicVolume = "BackgroundMusicVolume";
        public const string PlayerPrefSoundEffectsVolume = "SoundEffectsVolume";
        public const string PlayerPrefItemCollisionWhileDragged = "ItemCollisionWhileDragged";
        
        public static string SelectedWorld { get; set; } = "world.zen";
        public static string SelectedWaypoint { get; set; } = "START";

        // We need to set the scale so that collision and NPC animation is starting at the right spot.
        public static Vector3 VobZsScale = new(0.1f, 0.1f, 0.1f);

        // e.g. for NPCs to check if they reached a FreePoint already. Value is based on best guess/testing.
        public const float NpcDestinationReachedThreshold = 0.6f;
        public const float NpcRotationSpeed = 500f;

        public const string DaedalusHeroInstanceName = "PC_HERO"; // TODO - can be read from .ini file.


        public static int DaedalusAIVItemStatusKey;
        public static int DaedalusAIVItemFreqKey;

        public static int DaedalusTAITNone;

        
        public static string YesLabel = "Yes";
        public static string NoLabel = "No";
        
        static Constants()
        {
            LoadingMaterial = new Material(ShaderWorldLit);
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Init);
        }

        private static void Init()
        {
            DaedalusAIVItemStatusKey = GameData.GothicVm.GetSymbolByName("AIV_ITEMSTATUS").GetInt(0);
            DaedalusAIVItemFreqKey = GameData.GothicVm.GetSymbolByName("AIV_ITEMFREQ").GetInt(0);
            DaedalusTAITNone = GameData.GothicVm.GetSymbolByName("TA_IT_NONE").GetInt(0);
        }
    }
}
