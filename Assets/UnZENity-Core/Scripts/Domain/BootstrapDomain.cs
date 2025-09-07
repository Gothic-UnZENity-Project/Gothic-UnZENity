using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using GUZ.Core.Services;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Npc;
using GUZ.Core.Services.Player;
using GUZ.Core.Services.UI;
using GUZ.Core.Services.Vm;
using GUZ.Services.UI;
using Reflex.Attributes;
using ZenKit;
using ZenKit.Daedalus;

namespace GUZ.Core.Domain
{
    public class BootstrapDomain
    {
        [Inject] private readonly GameStateService _gameStateService;
        [Inject] private readonly LocalizationService _localizationService;
        [Inject] private readonly VmExternalService _vmExternalService;
        [Inject] private readonly NpcHelperService _npcHelperService;
        [Inject] private readonly VideoService _videoService;
        [Inject] private readonly FontService _fontService;
        
        public BootstrapDomain()
        {
            GlobalEventDispatcher.LoadGameStart.AddListener(LoadGothicVm);
        }
        
        public void Boot()
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
        private void SetLanguage()
        {
            // cs
            if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Bedna"))
                _localizationService.SetLanguage("cs", StringEncoding.CentralEurope);
            // pl
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Skrzynia"))
                _localizationService.SetLanguage("pl", StringEncoding.CentralEurope);
            // ru
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Коробка"))
                _localizationService.SetLanguage("ru", StringEncoding.EastEurope);
            // de
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Kiste"))
                _localizationService.SetLanguage("de", StringEncoding.WestEurope);
            // en - 2x as G1 vs G2 use a different value for it
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Crate", "Box"))
                _localizationService.SetLanguage("en", StringEncoding.WestEurope);
            // es
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Caja"))
                _localizationService.SetLanguage("es", StringEncoding.WestEurope);
            // fr
            else if (CheckEncoding(StringEncoding.EastEurope, "MOBNAME_CRATE", "Boite"))
                _localizationService.SetLanguage("fr", StringEncoding.WestEurope);
            // it
            else if (CheckEncoding(StringEncoding.CentralEurope, "MOBNAME_CRATE", "Cassa"))
                _localizationService.SetLanguage("it", StringEncoding.WestEurope);
            // Nothing found
            // TODO - Potentially re-enable error label on screen to say: We couldn't identify your language.
            // TODO - It also might make sense to manually overwrite/define language/Encoding via GameConfiguration.json - but only if needed in the future.
            // TODO - Add values for Gothic 2
            else
                throw new CultureNotFoundException("Language couldn't be identified based on current Gothic installation.");
        }

        private bool CheckEncoding(StringEncoding encoding, string daedalusConstantToCheck,
            params string[] valuesToCheck)
        {
            StringEncodingController.SetEncoding(encoding);

            var l10nSymbol = _gameStateService.GothicVm.GetSymbolByName(daedalusConstantToCheck)!;
            var l10nString = l10nSymbol.GetString(0);

            return valuesToCheck.Contains(l10nString);
        }

        public void LoadGothicVm()
        {
            _gameStateService.GothicVm = ResourceLoader.TryGetDaedalusVm("GOTHIC");

            _vmExternalService.RegisterExternals();
            _npcHelperService.Init();
        }
        
        private void LoadMiscVMs()
        {
            _gameStateService.FightVm = ResourceLoader.TryGetDaedalusVm("FIGHT");
            _gameStateService.MenuVm = ResourceLoader.TryGetDaedalusVm("MENU");
            _gameStateService.SfxVm = ResourceLoader.TryGetDaedalusVm("SFX");
            _gameStateService.PfxVm = ResourceLoader.TryGetDaedalusVm("PARTICLEFX");
        }

        /// <summary>
        /// We load all dialogs once and assign them to the appropriate NPCs once spawned.
        /// </summary>
        private void LoadDialogs()
        {
            var infoInstances = _gameStateService.GothicVm.GetInstanceSymbols("C_Info")
                .Select(symbol => _gameStateService.GothicVm.InitInstance<InfoInstance>(symbol))
                .ToList();

            infoInstances.ForEach(i => _gameStateService.Dialogs.Instances.Add(i));
        }

        private void LoadSubtitles()
        {
            var cutsceneSuffix = GameContext.ContextGameVersionService.CutsceneFileSuffix;
            var cutscenePath =
                $"{GameContext.ContextGameVersionService.RootPath}/_work/DATA/scripts/content/CUTSCENE/OU.{cutsceneSuffix}";
            _gameStateService.Dialogs.CutsceneLibrary = new(cutscenePath);
        }

        private void LoadVideos()
        {
            _videoService.InitVideos();
        }

        private void LoadFonts()
        {
            _fontService.Create();
        }

        private void LoadGuildData()
        {
            var guildMax = _gameStateService.GothicVm.GetSymbolByName("GIL_MAX");

            _gameStateService.GuildCount = guildMax?.GetInt(0) ?? 0;

            var tablesize = _gameStateService.GothicVm.GetSymbolByName("TAB_ANZAHL");
            _gameStateService.GuildTableSize = (int)(tablesize != null ? Math.Sqrt(tablesize.GetInt(0)) : 0);

            _gameStateService.GuildAttitudes = new int[_gameStateService.GuildCount * _gameStateService.GuildCount];

            var id = _gameStateService.GothicVm.GetSymbolByName("GIL_Values");
            if (id == null)
            {
                return;
            }

            _gameStateService.GuildValues = _gameStateService.GothicVm.InitInstance<GuildValuesInstance>(id);
            for (var i = 0; i < (int)VmService.Guild.GIL_PUBLIC; ++i)
            {
                _gameStateService.GuildValues.SetWaterDepthKnee(i, _gameStateService.GuildValues.GetWaterDepthKnee((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetWaterDepthChest(i, _gameStateService.GuildValues.GetWaterDepthChest((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetJumpUpHeight(i, _gameStateService.GuildValues.GetJumpUpHeight((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetSwimTime(i, _gameStateService.GuildValues.GetSwimTime((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetDiveTime(i, _gameStateService.GuildValues.GetDiveTime((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetStepHeight(i, _gameStateService.GuildValues.GetStepHeight((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetJumpLowHeight(i, _gameStateService.GuildValues.GetJumpLowHeight((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetJumpMidHeight(i, _gameStateService.GuildValues.GetJumpMidHeight((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetSlideAngle(i, _gameStateService.GuildValues.GetSlideAngle((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetSlideAngle2(i, _gameStateService.GuildValues.GetSlideAngle2((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetDisableAutoRoll(i, _gameStateService.GuildValues.GetDisableAutoRoll((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetSurfaceAlign(i, _gameStateService.GuildValues.GetSurfaceAlign((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetClimbHeadingAngle(i, _gameStateService.GuildValues.GetClimbHeadingAngle((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetClimbHorizAngle(i, _gameStateService.GuildValues.GetClimbHorizAngle((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetClimbGroundAngle(i, _gameStateService.GuildValues.GetClimbGroundAngle((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFightRangeBase(i, _gameStateService.GuildValues.GetFightRangeBase((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFightRangeFist(i, _gameStateService.GuildValues.GetFightRangeFist((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFightRangeG(i, _gameStateService.GuildValues.GetFightRangeG((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFightRange1Hs(i, _gameStateService.GuildValues.GetFightRange1Hs((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFightRange1Ha(i, _gameStateService.GuildValues.GetFightRange1Ha((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFightRange2Hs(i, _gameStateService.GuildValues.GetFightRange2Hs((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFightRange2Ha(i, _gameStateService.GuildValues.GetFightRange2Ha((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFallDownHeight(i, _gameStateService.GuildValues.GetFallDownHeight((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetFallDownDamage(i, _gameStateService.GuildValues.GetFallDownDamage((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetBloodDisabled(i, _gameStateService.GuildValues.GetBloodDisabled((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetBloodMaxDistance(i, _gameStateService.GuildValues.GetBloodMaxDistance((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetBloodAmount(i, _gameStateService.GuildValues.GetBloodAmount((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetBloodFlow(i, _gameStateService.GuildValues.GetBloodFlow((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetTurnSpeed(i, _gameStateService.GuildValues.GetTurnSpeed((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetBloodEmitter(i, _gameStateService.GuildValues.GetBloodEmitter((int)VmService.Guild.GIL_HUMAN));
                _gameStateService.GuildValues.SetBloodTexture(i, _gameStateService.GuildValues.GetBloodTexture((int)VmService.Guild.GIL_HUMAN,0)); //TODO: PR FIX to zenkitcs, getter should have only 1 param not 2
            }
        }
    }
}
