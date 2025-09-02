using System;
using System.Globalization;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Globals;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Npc;
using GUZ.Core.Services.Vm;
using GUZ.Core.Vm;
using ZenKit;
using ZenKit.Daedalus;
using static GUZ.Core.Globals.DaedalusConst;

namespace GUZ.Core.Manager
{
    public static class Bootstrapper
    {
        static Bootstrapper()
        {
            GlobalEventDispatcher.LoadGameStart.AddListener(LoadGothicVm);
        }
        
        public static void OnApplicationQuit()
        {
            GameData.Dispose();
            VmInstanceManager.Dispose();
        }

        public static void Boot()
        {
            LoadGothicVm();
            LoadMiscVMs();
            SetLanguage();
            LoadDialogs();
            LoadSubtitles();
            LoadVideos();
            LoadFonts();
            LoadGuildData();

            GameContext.IsZenKitInitialized = true;
            GlobalEventDispatcher.ZenKitBootstrapped.Invoke();
        }

        /// <summary>
        /// We set language based on one of the first Daedalus string constants loaded inside DaedalusVm.
        /// We check for expected values with different StringEncoding values. As they're always converted to UTF-8,
        /// we can easily check them for a match.
        /// </summary>
        private static void SetLanguage()
        {
            // cs
            if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Bedna"))
                GameGlobals.Localization.SetLanguage("cs", StringEncoding.CentralEurope);
            // pl
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Skrzynia"))
                GameGlobals.Localization.SetLanguage("pl", StringEncoding.CentralEurope);
            // ru
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Коробка"))
                GameGlobals.Localization.SetLanguage("ru", StringEncoding.EastEurope);
            // de
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Kiste"))
                GameGlobals.Localization.SetLanguage("de", StringEncoding.WestEurope);
            // en - 2x as G1 vs G2 use a different value for it
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Crate", "Box"))
                GameGlobals.Localization.SetLanguage("en", StringEncoding.WestEurope);
            // es
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Caja"))
                GameGlobals.Localization.SetLanguage("es", StringEncoding.WestEurope);
            // fr
            else if (CheckEncoding(StringEncoding.EastEurope, "MOBNAME_CRATE", "Boite"))
                GameGlobals.Localization.SetLanguage("fr", StringEncoding.WestEurope);
            // it
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Cassa"))
                GameGlobals.Localization.SetLanguage("it", StringEncoding.WestEurope);
            // Nothing found
            // TODO - Potentially re-enable error label on screen to say: We couldn't identify your language.
            // TODO - It also might make sense to manually overwrite/define language/Encoding via GameConfiguration.json - but only if needed in the future.
            // TODO - Add values for Gothic 2
            else
                throw new CultureNotFoundException("Language couldn't be identified based on current Gothic installation.");
        }

        private static bool CheckEncoding(StringEncoding encoding, string daedalusConstantToCheck,
            params string[] valuesToCheck)
        {
            StringEncodingController.SetEncoding(encoding);

            var l10nSymbol = GameData.GothicVm.GetSymbolByName(daedalusConstantToCheck)!;
            var l10nString = l10nSymbol.GetString(0);

            return valuesToCheck.Contains(l10nString);
        }

        public static void LoadGothicVm()
        {
            GameData.GothicVm = ResourceLoader.TryGetDaedalusVm("GOTHIC");

            // FIXME - Shoould be loaded by [Inject] instead!
            ReflexProjectInstaller.DIContainer.Resolve<VmService>().RegisterExternals();
            // FIXME - Shoould be loaded by [Inject] instead!
            ReflexProjectInstaller.DIContainer.Resolve<NpcHelperService>().Init();
        }
        
        private static void LoadMiscVMs()
        {
            GameData.FightVm = ResourceLoader.TryGetDaedalusVm("FIGHT");
            GameData.MenuVm = ResourceLoader.TryGetDaedalusVm("MENU");
            GameData.SfxVm = ResourceLoader.TryGetDaedalusVm("SFX");
            GameData.PfxVm = ResourceLoader.TryGetDaedalusVm("PARTICLEFX");
        }

        /// <summary>
        /// We load all dialogs once and assign them to the appropriate NPCs once spawned.
        /// </summary>
        private static void LoadDialogs()
        {
            var infoInstances = GameData.GothicVm.GetInstanceSymbols("C_Info")
                .Select(symbol => GameData.GothicVm.InitInstance<InfoInstance>(symbol))
                .ToList();

            infoInstances.ForEach(i => GameData.Dialogs.Instances.Add(i));
        }

        private static void LoadSubtitles()
        {
            var cutsceneSuffix = GameContext.ContextGameVersionService.CutsceneFileSuffix;
            var cutscenePath =
                $"{GameContext.ContextGameVersionService.RootPath}/_work/DATA/scripts/content/CUTSCENE/OU.{cutsceneSuffix}";
            GameData.Dialogs.CutsceneLibrary = new(cutscenePath);
        }

        private static void LoadVideos()
        {
            GameGlobals.Video.InitVideos();
        }

        private static void LoadFonts()
        {
            GameGlobals.Font.Create();
        }

        private static void LoadGuildData()
        {
            var guildMax = GameData.GothicVm.GetSymbolByName("GIL_MAX");

            GameData.GuildCount = guildMax?.GetInt(0) ?? 0;

            var tablesize = GameData.GothicVm.GetSymbolByName("TAB_ANZAHL");
            GameData.GuildTableSize = (int)(tablesize != null ? Math.Sqrt(tablesize.GetInt(0)) : 0);

            GameData.GuildAttitudes = new int[GameData.GuildCount * GameData.GuildCount];

            var id = GameData.GothicVm.GetSymbolByName("GIL_Values");
            if (id == null)
            {
                return;
            }

            GameData.GuildValues = GameData.GothicVm.InitInstance<GuildValuesInstance>(id);
            for (var i = 0; i < (int)Guild.GIL_PUBLIC; ++i)
            {
                GameData.GuildValues.SetWaterDepthKnee(i, GameData.GuildValues.GetWaterDepthKnee((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetWaterDepthChest(i, GameData.GuildValues.GetWaterDepthChest((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetJumpUpHeight(i, GameData.GuildValues.GetJumpUpHeight((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetSwimTime(i, GameData.GuildValues.GetSwimTime((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetDiveTime(i, GameData.GuildValues.GetDiveTime((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetStepHeight(i, GameData.GuildValues.GetStepHeight((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetJumpLowHeight(i, GameData.GuildValues.GetJumpLowHeight((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetJumpMidHeight(i, GameData.GuildValues.GetJumpMidHeight((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetSlideAngle(i, GameData.GuildValues.GetSlideAngle((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetSlideAngle2(i, GameData.GuildValues.GetSlideAngle2((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetDisableAutoRoll(i, GameData.GuildValues.GetDisableAutoRoll((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetSurfaceAlign(i, GameData.GuildValues.GetSurfaceAlign((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetClimbHeadingAngle(i, GameData.GuildValues.GetClimbHeadingAngle((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetClimbHorizAngle(i, GameData.GuildValues.GetClimbHorizAngle((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetClimbGroundAngle(i, GameData.GuildValues.GetClimbGroundAngle((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFightRangeBase(i, GameData.GuildValues.GetFightRangeBase((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFightRangeFist(i, GameData.GuildValues.GetFightRangeFist((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFightRangeG(i, GameData.GuildValues.GetFightRangeG((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFightRange1Hs(i, GameData.GuildValues.GetFightRange1Hs((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFightRange1Ha(i, GameData.GuildValues.GetFightRange1Ha((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFightRange2Hs(i, GameData.GuildValues.GetFightRange2Hs((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFightRange2Ha(i, GameData.GuildValues.GetFightRange2Ha((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFallDownHeight(i, GameData.GuildValues.GetFallDownHeight((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetFallDownDamage(i, GameData.GuildValues.GetFallDownDamage((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetBloodDisabled(i, GameData.GuildValues.GetBloodDisabled((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetBloodMaxDistance(i, GameData.GuildValues.GetBloodMaxDistance((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetBloodAmount(i, GameData.GuildValues.GetBloodAmount((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetBloodFlow(i, GameData.GuildValues.GetBloodFlow((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetTurnSpeed(i, GameData.GuildValues.GetTurnSpeed((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetBloodEmitter(i, GameData.GuildValues.GetBloodEmitter((int)Guild.GIL_HUMAN));
                GameData.GuildValues.SetBloodTexture(i, GameData.GuildValues.GetBloodTexture((int)Guild.GIL_HUMAN,0)); //TODO: PR FIX to zenkitcs, getter should have only 1 param not 2
            }
        }
    }
}
