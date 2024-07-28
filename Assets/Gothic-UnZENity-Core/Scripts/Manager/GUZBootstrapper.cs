using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Globals;
using GUZ.Core.Vm;
using ZenKit;
using ZenKit.Daedalus;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager
{
    public class GuzBootstrapper
    {
        public static void OnApplicationQuit()
        {
            GameData.Dispose();
            VmInstanceManager.Dispose();
            TextureCache.Dispose();
            LookupCache.Dispose();
            MorphMeshCache.Dispose();
        }

        public static void BootGothicUnZeNity(GameConfiguration config, string g1Dir)
        {
            var watch = Stopwatch.StartNew();

            GuzContext.SetContext(config.GameControls);

            LoadGothicVm(g1Dir);
            SetLanguage();
            LoadDialogs();
            LoadSfxVm(g1Dir);
            LoadPfxVm(g1Dir);
            LoadFonts();

            watch.Stop();
            Debug.Log($"Time spent for Bootstrapping ZenKit: {watch.Elapsed}");

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
                Debug.Log($"Selecting StringEncoding={StringEncoding.CentralEurope}");
                StringEncodingController.SetEncoding(StringEncoding.CentralEurope);
            }
            // ru
            else if (CheckEncoding(StringEncoding.EastEurope, "MOBNAME_CRATE", "Коробка"))
            {
                Debug.Log($"Selecting StringEncoding={StringEncoding.EastEurope}");
                StringEncodingController.SetEncoding(StringEncoding.EastEurope);
            }
            // de, en, es, fr, it
            else if (CheckEncoding(StringEncoding.WestEurope, "MOBNAME_CRATE", "Kiste", "Box", "Caja", "Boite", "Cassa"))
            {
                Debug.Log($"Selecting StringEncoding={StringEncoding.WestEurope}");
                StringEncodingController.SetEncoding(StringEncoding.WestEurope);
            }
            // Nothing found
            // TODO - Potentially re-enable error label on screen to say: We couldn't identify your language.
            // TODO - It also might make sense to manually overwrite/define language/Encoding via GameConfiguration.json - but only if needed in the future.
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

        private static void LoadGothicVm(string g1Dir)
        {
            GameData.GothicVm = ResourceLoader.TryGetDaedalusVm("GOTHIC");

            NpcHelper.LoadHero();

            VmGothicExternals.RegisterExternals();
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

        private static void LoadSfxVm(string g1Dir)
        {
            GameData.SfxVm = ResourceLoader.TryGetDaedalusVm("SFX");
        }

        private static void LoadPfxVm(string g1Dir)
        {
            GameData.PfxVm = ResourceLoader.TryGetDaedalusVm("PARTICLEFX");
        }

        private static void LoadFonts()
        {
            GameGlobals.Font.Create();
        }
    }
}
