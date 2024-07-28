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

        //Layer for all Items, specifically to disable collision physics between player and items
        public static LayerMask PlayerLayer = LayerMask.NameToLayer("Player");
        public static LayerMask ItemLayer = LayerMask.NameToLayer("Item");

        // set layer to interactive so we can interact using XR Ray interactor
        public static LayerMask InteractiveLayer = LayerMask.NameToLayer("Interactive");

        // solves some weird interactions between the teleport raycast and collider (musicZone/worldTriggerChange)
        public static LayerMask IgnoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        // Tags
        public const string ClimbableTag = "Climbable";
        public const string SpotTag = "PxVob_zCVobSpot";
        public const string PlayerTag = "Player";

        public static int MeshPerFrame { get; } = 10;
        public static int VobsPerFrame { get; } = 75;
        public static int NpcsPerFrame { get; } = 75;

        //Collection of PlayerPref entries for settings
        public const string MoveSpeedPlayerPref = "MoveSpeed";
        public const string TurnSettingPlayerPref = "TurnSetting";
        public const string MusicVolumePlayerPref = "BackgroundMusicVolume";
        public const string SoundEffectsVolumePlayerPref = "SoundEffectsVolume";
        public static float MoveSpeed { get; set; } = 8f;

        public static string SelectedWorld { get; set; } = "world.zen";
        public static string SelectedWaypoint { get; set; } = "START";

        // We need to set the scale so that collision and NPC animation is starting at the right spot.
        public static Vector3 VobZsScale = new(0.1f, 0.1f, 0.1f);

        // e.g. for NPCs to check if they reached a FreePoint already. Value is based on best guess/testing.
        public const float CloseToThreshold = 0.6f;

        public const string DaedalusHeroInstanceName = "PC_HERO"; // TODO - can be read from .ini file.


        public static int DaedalusAIVItemStatusKey;
        public static int DaedalusAIVItemFreqKey;

        public static int DaedalusTAITNone;

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
