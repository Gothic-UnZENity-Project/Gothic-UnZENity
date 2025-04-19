using System;
using System.Globalization;
using System.Linq;
using System.Text;
using GUZ.Core.Caches;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using ZenKit;
using ZenKit.Daedalus;
using static GUZ.Core.Globals.Constants;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager
{
    public class GuzBootstrapper
    {
        public static void OnApplicationQuit()
        {
            GameData.Dispose();
            VmInstanceManager.Dispose();
            TextureCache.Dispose();
            MultiTypeCache.Dispose();
            MorphMeshCache.Dispose();
        }

        public static void BootGothicUnZeNity()
        {
            LoadVMs();
            SetLanguage();
            LoadDialogs();
            LoadSubtitles();
            LoadVideos();
            LoadFonts();
            LoadGuildData();

            GlobalEventDispatcher.ZenKitBootstrapped.Invoke();
        }

        /// <summary>
        /// We set language based on one of the first Daedalus string constants loaded inside DaedalusVm.
        /// We check for expected values with different StringEncoding values. As they're always converted to UTF-8,
        /// we can easily check them for a match.
        /// </summary>
        public static void SetLanguage()
        {
            // cs, pl
            if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Bedna", "Skrzynia"))
            {
                Logger.Log($"Selecting StringEncoding={StringEncoding.CentralEurope}", LogCat.Loading);
                StringEncodingController.SetEncoding(StringEncoding.CentralEurope);

                GameData.Encoding = Encoding.GetEncoding((int)StringEncoding.CentralEurope);;
            }
            // ru
            else if (CheckEncoding(StringEncoding.EastEurope, "MOBNAME_CRATE", "Коробка"))
            {
                Logger.Log($"Selecting StringEncoding={StringEncoding.EastEurope}", LogCat.Loading);
                StringEncodingController.SetEncoding(StringEncoding.EastEurope);

                GameData.Encoding = Encoding.GetEncoding((int)StringEncoding.EastEurope);
            }
            // de, en (2x), es, fr, it
            else if (CheckEncoding(StringEncoding.WestEurope, "MOBNAME_CRATE", "Kiste", "Crate", "Box", "Caja", "Boite", "Cassa"))
            {
                Logger.Log($"Selecting StringEncoding={StringEncoding.WestEurope}", LogCat.Loading);
                StringEncodingController.SetEncoding(StringEncoding.WestEurope);

                GameData.Encoding = Encoding.GetEncoding((int)StringEncoding.WestEurope);
            }
            // Nothing found
            // TODO - Potentially re-enable error label on screen to say: We couldn't identify your language.
            // TODO - It also might make sense to manually overwrite/define language/Encoding via GameConfiguration.json - but only if needed in the future.
            // TODO - Add values for Gothic 2
            else
            {
                throw new CultureNotFoundException("Language couldn't be identified based on current Gothic installation.");
            }
        }

        private static bool CheckEncoding(StringEncoding encoding, string daedalusConstantToCheck,
            params string[] valuesToCheck)
        {
            StringEncodingController.SetEncoding(encoding);

            var l10nSymbol = GameData.GothicVm.GetSymbolByName(daedalusConstantToCheck);
            var l10nString = l10nSymbol.GetString(0);

            return valuesToCheck.Contains(l10nString);
        }

        private static void LoadVMs()
        {
            GameData.GothicVm = ResourceLoader.TryGetDaedalusVm("GOTHIC");
            VmGothicExternals.RegisterExternals();
            NpcHelper.Init();

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
            string cutsceneSuffix = GameContext.GameVersionAdapter.CutsceneFileSuffix;
            string cutscenePath =
                $"{GameContext.GameVersionAdapter.RootPath}/_work/DATA/scripts/content/CUTSCENE/OU.{cutsceneSuffix}";
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

            GameData.cGuildValue = GameData.GothicVm.InitInstance<GuildValuesInstance>(id);
            for (var i = 0; i < (int)Guild.GIL_PUBLIC; ++i)
            {
                GameData.cGuildValue.SetWaterDepthKnee(i, GameData.cGuildValue.GetWaterDepthKnee((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetWaterDepthChest(i, GameData.cGuildValue.GetWaterDepthChest((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetJumpUpHeight(i, GameData.cGuildValue.GetJumpUpHeight((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetSwimTime(i, GameData.cGuildValue.GetSwimTime((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetDiveTime(i, GameData.cGuildValue.GetDiveTime((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetStepHeight(i, GameData.cGuildValue.GetStepHeight((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetJumpLowHeight(i, GameData.cGuildValue.GetJumpLowHeight((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetJumpMidHeight(i, GameData.cGuildValue.GetJumpMidHeight((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetSlideAngle(i, GameData.cGuildValue.GetSlideAngle((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetSlideAngle2(i, GameData.cGuildValue.GetSlideAngle2((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetDisableAutoRoll(i, GameData.cGuildValue.GetDisableAutoRoll((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetSurfaceAlign(i, GameData.cGuildValue.GetSurfaceAlign((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetClimbHeadingAngle(i, GameData.cGuildValue.GetClimbHeadingAngle((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetClimbHorizAngle(i, GameData.cGuildValue.GetClimbHorizAngle((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetClimbGroundAngle(i, GameData.cGuildValue.GetClimbGroundAngle((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFightRangeBase(i, GameData.cGuildValue.GetFightRangeBase((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFightRangeFist(i, GameData.cGuildValue.GetFightRangeFist((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFightRangeG(i, GameData.cGuildValue.GetFightRangeG((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFightRange1Hs(i, GameData.cGuildValue.GetFightRange1Hs((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFightRange1Ha(i, GameData.cGuildValue.GetFightRange1Ha((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFightRange2Hs(i, GameData.cGuildValue.GetFightRange2Hs((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFightRange2Ha(i, GameData.cGuildValue.GetFightRange2Ha((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFallDownHeight(i, GameData.cGuildValue.GetFallDownHeight((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetFallDownDamage(i, GameData.cGuildValue.GetFallDownDamage((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetBloodDisabled(i, GameData.cGuildValue.GetBloodDisabled((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetBloodMaxDistance(i, GameData.cGuildValue.GetBloodMaxDistance((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetBloodAmount(i, GameData.cGuildValue.GetBloodAmount((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetBloodFlow(i, GameData.cGuildValue.GetBloodFlow((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetTurnSpeed(i, GameData.cGuildValue.GetTurnSpeed((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetBloodEmitter(i, GameData.cGuildValue.GetBloodEmitter((int)Guild.GIL_HUMAN));
                GameData.cGuildValue.SetBloodTexture(i, GameData.cGuildValue.GetBloodTexture((int)Guild.GIL_HUMAN,0)); //TODO: PR FIX to zenkitcs, getter should have only 1 param not 2
            }
        }
    }
}
