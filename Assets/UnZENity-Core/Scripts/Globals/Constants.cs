using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using UnityEngine;
using ZenKit.Vobs;
using Material = UnityEngine.Material;

namespace GUZ.Core.Globals
{
    public static class Constants
    {
        public static class DaedalusMenu
        {
            
            public static readonly string[] DisabledGothicMenuSettings =
            {
                // Whole submenus
                "MENUITEM_OPT_VIDEO", "MENUITEM_OPT_CONTROLS",
                // Parts of main SETTINGS menu
                "MENUITEM_PERF", "MENUITEM_PERF_CHOICE",
                // Parts of GAME menu
                "MENUITEM_GAME_ANIMATE_WINDOWS", "MENUITEM_GAME_ANIMATE_WINDOWS_CHOICE",
                "MENUITEM_GAME_LOOKAROUND_INVERSE", "MENUITEM_GAME_LOOKAROUND_INVERSE_CHOICE",
                "MENUITEM_M", "MENUITEM_M_CHOICE",
                "MENUITEM_MSENSITIVITY", "MENUITEM_MSENSITIVITY_SLIDER",
                "MENUITEM_GAME_BLOOD", "MENUITEM_GAME_BLOOD_CHOICE",
                // Parts of GRAPHICS menu
                "MENUITEM_GRA_TEXQUAL", "MENUITEM_GRA_TEXQUAL_SLIDER",
                "MENUITEM_GRA_MODEL_DETAIL", "MENUITEM_GRA_MODEL_DETAIL_SLIDER",
                "MENUITEM_GRA_SKY_EFFECTS", "MENUITEM_GRA_SKY_EFFECTS_CHOICE",
                // Parts of AUDIO menu
                "MENUITEM_AUDIO_SFXVOL", "MENUITEM_AUDIO_SFXVOL_SLIDER",
                "MENUITEM_AUDIO_PROVIDER", "MENUITEM_AUDIO_PROVIDER_CHOICE",
                "MENUITEM_AUDIO_SPEEKER", "MENUITEM_AUDIO_SPEEKER_CHOICE",
                "MENUITEM_AUDIO_REVERB", "MENUITEM_AUDIO_REVERB_CHOICE",
                "MENUITEM_AUDIO_SAMPLERATE", "MENUITEM_AUDIO_SAMPLERATE_CHOICE"
            };
            
            public static int MaxUserStrings => GameData.MenuVm.GetSymbolInt("MAX_USERSTRINGS");
            public static int MaxItems => GameData.MenuVm.GetSymbolInt("MAX_ITEMS");
            public static int MaxEvent => GameData.MenuVm.GetSymbolInt("MAX_EVENTS");
            public static int MaxSelActions => GameData.MenuVm.GetSymbolInt("MAX_SEL_ACTIONS");
            public static int MaxUserVars => GameData.MenuVm.GetSymbolInt("MAX_USERVARS");
            
            public static string BackPic => GameData.MenuVm.GetSymbolString("MENU_BACK_PIC");

            public static int MenuStartY => GameData.MenuVm.GetSymbolInt("MENU_START_Y");
            public static int MenuDY => GameData.MenuVm.GetSymbolInt("MENU_DY");
        }
        
        public static class Daedalus
        {
            public static string DoorLockSoundName => "DOOR_LOCK.WAV";
            public static string PickLockFailureSoundName => GameData.GothicVm.GetSymbolByName("_STR_SOUND_PICKLOCK_FAILURE").GetString(0);
            public static string PickLockBrokenSoundName => GameData.GothicVm.GetSymbolByName("_STR_SOUND_PICKLOCK_BROKEN").GetString(0);
            public static string PickLockSuccessSoundName => GameData.GothicVm.GetSymbolByName("_STR_SOUND_PICKLOCK_SUCCESS").GetString(0);
            public static string PickLockUnlockSoundName => GameData.GothicVm.GetSymbolByName("_STR_SOUND_PICKLOCK_UNLOCK").GetString(0);
            public static string DoorUnlockSoundName => "DOOR_UNLOCK.WAV"; // _STR_*_UNLOCK value above couldn't be found/isn't used in G1, therefore we use this as fallback.


            public static string[] MobSit => GameData.GothicVm.GetSymbolByName("MOB_SIT").GetString(0).Split(',');
            public static string[] MobLie => GameData.GothicVm.GetSymbolByName("MOB_LIE").GetString(0).Split(',');
            public static string[] MobClimb => GameData.GothicVm.GetSymbolByName("MOB_CLIMB").GetString(0).Split(',');
            public static string[] MobNotInterruptable => GameData.GothicVm.GetSymbolByName("MOB_NOTINTERRUPTABLE").GetString(0).Split(',');
            
            public static List<string> TalentTitles
            {
                get
                {
                    var talents = GameData.GothicVm.GetSymbolByName("TXT_TALENTS");;
                    var talentCount = GameData.GothicVm.GetSymbolByName("NPC_TALENT_MAX").GetInt(0);
                    return Enumerable.Range(0, talentCount).Select(i => talents.GetString((ushort)i)).ToList();
                }
            }

            public static List<string> TalentSkills
            {
                get
                {
                    var talentSkills = GameData.GothicVm.GetSymbolByName("TXT_TALENTS_SKILLS");;
                    var talentCount = GameData.GothicVm.GetSymbolByName("NPC_TALENT_MAX").GetInt(0);
                    return Enumerable.Range(0, talentCount).Select(i => talentSkills.GetString((ushort)i)).ToList();
                }
            }
        }

        public static class Animations
        {
            public const string RootBoneName = "BIP01";
        }

        // Unity shaders
        public static readonly Shader ShaderUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        public static readonly Shader ShaderUnlitParticles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        public static readonly Shader ShaderTMPSprite = Shader.Find("TextMeshPro/Sprite");
        public static readonly Shader ShaderDecal = Shader.Find("Shader Graphs/Decal");
        public static readonly Shader ShaderStandard = Shader.Find("Standard");

        public static readonly Material LoadingMaterial; // Used for Vobs and World before applying TextureArray.
        public static readonly Material DebugMaterial = new(ShaderUnlit); // Used for Marvin mode elements like visible WayPoints.

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

        public static readonly Texture2D TextureUnZENityLogo = Resources.Load<Texture2D>("Gothic-UnZENity-logo");
        public static readonly Texture2D TextureUnZENityLogoTransparent = Resources.Load<Texture2D>("Gothic-UnZENity-logo-transparent");
        public static readonly Texture2D TextureUnZENityLogoInverse = Resources.Load<Texture2D>("Gothic-UnZENity-logo-inverse");
        public static readonly Texture2D TextureUnZENityLogoInverseTransparent = Resources.Load<Texture2D>("Gothic-UnZENity-logo-inverse-transparent");


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
        public const string ScenePlayer = "Player";
        public const string SceneGameVersion = "GameVersion";
        public const string ScenePreCaching = "PreCaching";
        public const string SceneLogo = "Logo";
        public const string SceneMainMenu = "MainMenu";
        public const string SceneLoading = "Loading";
        public const string SceneLab = "Lab";


        // Hint: We will never be able to cache lights as their radius is always dynamic.
        public static readonly VirtualObjectType[] StaticCacheVobTypes =
        {
            VirtualObjectType.oCItem,
            VirtualObjectType.oCMOB,
            VirtualObjectType.oCMobBed,
            VirtualObjectType.oCMobContainer,
            VirtualObjectType.oCMobDoor,
            VirtualObjectType.oCMobFire,
            VirtualObjectType.oCMobInter,
            VirtualObjectType.oCMobLadder,
            VirtualObjectType.oCMobSwitch,
            VirtualObjectType.oCMobWheel,
            VirtualObjectType.zCVob,
            VirtualObjectType.zCVobAnimate,
            VirtualObjectType.zCVobStair
        };

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

        // Default Tags
        public const string PlayerTag = "Player";
        public const string MainCameraTag = "MainCamera";

        // Custom Tags
        public const string VobTag = "GUZVob";

        
        // FIXME - load from INI file!
        public static string SelectedWorld { get; set; } = "world.zen";
        public static string SelectedWaypoint { get; set; } = "START";

        // We need to set the scale so that collision and NPC animation is starting at the right spot.
        public static Vector3 VobZsScale = new(0.1f, 0.1f, 0.1f);

        // e.g. for NPCs to check if they reached a FreePoint already. Value is based on best guess/testing.
        public const float NpcDestinationReachedThreshold = 0.6f;
        public const float NpcRotationSpeed = 500f;

        public const string DaedalusHeroInstanceName = "PC_HERO"; // TODO - can be read from .ini file.

        // Alter this value to enforce game to recreate cache during next start.
        public const string StaticCacheVersion = "2";

        /// <summary>
        /// Used during pre-caching to calculate world chunks to merge.
        /// We implemented a logic to overcome 9 lights on a mesh - limitation by URP forward rendering.
        /// If you change this value, you also need to alter it inside Shader: StationaryLighting.hlsl --> MAX_AFFECTING_STATIONARY_LIGHTS
        /// </summary>
        public const int MaxLightsPerWorldChunk = 16;

        public static class DaedalusConst
        {
            public static int AIVMMRealId => GameData.GothicVm.GetSymbolByName("AIV_MM_REAL_ID").GetInt(0);
            public static int AIVInvincibleKey => GameData.GothicVm.GetSymbolByName("AIV_INVINCIBLE").GetInt(0);
            public static int AIVItemStatusKey => GameData.GothicVm.GetSymbolByName("AIV_ITEMSTATUS").GetInt(0);
            public static int AIVItemFreqKey => GameData.GothicVm.GetSymbolByName("AIV_ITEMFREQ").GetInt(0);
            public static int TAITNone => GameData.GothicVm.GetSymbolByName("TA_IT_NONE").GetInt(0);
        }
        
        
        /*
         * Colors
         */
        
        public static Color TextNormalColor = new Color(1, 1, 1, 1);
        public static Color TextDisabledColor = new Color(1, 1, 1, 0.4f);
        public static Color TextRedColor = new Color(1, 0, 0, 1f);
        public static Color TextYellowColor = new Color(1, 1, 0, 1);
        
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
        }
    }
}
