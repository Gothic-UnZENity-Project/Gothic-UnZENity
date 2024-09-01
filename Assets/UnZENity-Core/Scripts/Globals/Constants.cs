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

        // Custom shaders
        // For textures like NPCs, _not_ the grouped texture array.
        public static readonly string ShaderSingleMeshLitName = "Lit/SingleMesh";
        public static readonly Shader ShaderSingleMeshLit = Shader.Find(ShaderSingleMeshLitName);
        public static readonly Shader ShaderSingleMeshLitDynamic = Shader.Find("Lit/SingleMesh-Dynamic");

        public static readonly string ShaderWorldLitName = "Lit/World";
        public static readonly Shader ShaderWorldLit = Shader.Find(ShaderWorldLitName);
        public static readonly Shader ShaderLitAlphaToCoverage = Shader.Find("Lit/AlphaToCoverage");
        public static readonly Shader ShaderWater = Shader.Find("Lit/Water");
        public static readonly Shader ShaderBarrier = Shader.Find("Unlit/Barrier");
        public static readonly Shader ShaderThunder = Shader.Find("Unlit/ThunderShader");


        /*
         * Shader properties
         */

        public static readonly int ShaderTypeDefault = 0;
        public static readonly int ShaderTypeTransparent = 3000;

        // Interactable VOB is in focus of player. Brighten up the color by this value.
        public static readonly int ShaderPropertyFocusBrightness = Shader.PropertyToID("_FocusBrightness");
        public static readonly float ShaderPropertyFocusBrightnessDefault = 1f;
        public static readonly float ShaderPropertyFocusBrightnessValue = 10f;

        // Item is a "ghost" in hand. i.e. no collision.
        public static readonly int ShaderPropertyTransparency = Shader.PropertyToID("_Alpha");
        public static readonly float ShaderPropertyTransparencyDefault = 1f;
        public static readonly float ShaderPropertyTransparencyValue = 0.60f;

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

        /*
         * ### Tags
         */
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
        public const string PlayerPrefMusicVolume = "MusicVolume";
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
        
        public enum Guild
        {
            GIL_NONE = 0, // (keine)
            GIL_HUMAN = 1, // Special Guild -> To set Constants for ALL Human Guilds --> wird verwendet in Species.d
            GIL_PAL = 1, // Paladin
            GIL_MIL = 2, // Miliz
            GIL_VLK = 3, // Bürger
            GIL_KDF = 4, // Magier
            GIL_NOV = 5, // Magier Novize
            GIL_DJG = 6, // Drachenjäger
            GIL_SLD = 7, // Söldner
            GIL_BAU = 8, // Bauer
            GIL_BDT = 9, // Bandit
            GIL_STRF = 10, // Prisoner, Sträfling
            GIL_DMT = 11, // Dementoren
            GIL_OUT = 12, // Outlander (z.B. kleine Bauernhöfe)
            GIL_PIR = 13, // Pirat
            GIL_KDW = 14, // KDW
            GIL_EMPTY_D = 15, // NICHT VERWENDEN!

            //-----------------------------------------------
            GIL_PUBLIC = 15, // für öffentliche Portalräume

            //-----------------------------------------------
            GIL_SEPERATOR_HUM = 16,
            GIL_MEATBUG = 17,
            GIL_SHEEP = 18,
            GIL_GOBBO = 19, // Green Goblin / Black Goblin
            GIL_GOBBO_SKELETON = 20,
            GIL_SUMMONED_GOBBO_SKELETON = 21,
            GIL_SCAVENGER = 22, // (bei Bedarf) Scavenger / Evil Scavenger /OrcBiter
            GIL_GIANT_RAT = 23,
            GIL_GIANT_BUG = 24,
            GIL_BLOODFLY = 25,
            GIL_WARAN = 26, // Waren / Feuerwaran
            GIL_WOLF = 27, // Wolf / Warg
            GIL_SUMMONED_WOLF = 28,
            GIL_MINECRAWLER = 29, // Minecrawler / Minecrawler Warrior
            GIL_LURKER = 30,
            GIL_SKELETON = 31,
            GIL_SUMMONED_SKELETON = 32,
            GIL_SKELETON_MAGE = 33,
            GIL_ZOMBIE = 34,
            GIL_SNAPPER = 35, // Snapper / Dragon Snapper /Razor
            GIL_SHADOWBEAST = 36, //Shadowbeast / Bloodhound
            GIL_SHADOWBEAST_SKELETON = 37,
            GIL_HARPY = 38,
            GIL_STONEGOLEM = 39,
            GIL_FIREGOLEM = 40,
            GIL_ICEGOLEM = 41,
            GIL_SUMMONED_GOLEM = 42,
            GIL_DEMON = 43,
            GIL_SUMMONED_DEMON = 44,
            GIL_TROLL = 45, // Troll / Schwarzer Troll
            GIL_SWAMPSHARK = 46, // (bei Bedarf)
            GIL_DRAGON = 47, // Feuerdrache / Eisdrache / Felsdrache / Sumpfdrache / Untoter Drache
            GIL_MOLERAT = 48, // Molerat
            GIL_ALLIGATOR = 49,
            GIL_SWAMPGOLEM = 50,
            GIL_Stoneguardian = 51,
            GIL_Gargoyle = 52,
            GIL_Empty_A = 53,
            GIL_SummonedGuardian = 54,
            GIL_SummonedZombie = 55,
            GIL_EMPTY_B = 56,
            GIL_EMPTY_C = 57,
            GIL_SEPERATOR_ORC = 58, // (ehem. 37)
            GIL_ORC = 59, // Ork-Krieger / Ork-Shamane / Ork-Elite
            GIL_FRIENDLY_ORC = 60, // Ork-Sklave / Ur-Shak
            GIL_UNDEADORC = 61,
            GIL_DRACONIAN = 62,
            GIL_EMPTY_X = 63,
            GIL_EMPTY_Y = 64,
            GIL_EMPTY_Z = 65,
            GIL_MAX = 66
        }

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
