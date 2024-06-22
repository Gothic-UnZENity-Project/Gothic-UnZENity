using System.Diagnostics;
using System.IO;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Debugging;
using GUZ.Core.Globals;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Vm;
using GUZ.Core;
using ZenKit;
using ZenKit.Daedalus;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager
{
    public class GUZBootstrapper
    {
        public static void OnApplicationQuit()
        {
            GameData.Dispose();
            VmInstanceManager.Dispose();
            TextureCache.Dispose();
            LookupCache.Dispose();
            MorphMeshCache.Dispose();
        }
        
        public static void BootGothicUnZENity(GameConfiguration config, string g1Dir, string language)
        {
            var watch = Stopwatch.StartNew();

            GUZContext.SetContext(config.gameControls);
            
            SetLanguage(language);
            LoadGothicVm(g1Dir);
            LoadDialogs();
            LoadSfxVm(g1Dir);
            LoadPfxVm(g1Dir);
            LoadFonts();
            
            watch.Stop();
            Debug.Log($"Time spent for Bootstrapping ZenKit: {watch.Elapsed}");

            GlobalEventDispatcher.ZenKitBootstrapped.Invoke();
        }

        public static void SetLanguage(string language)
        {
            switch (language)
            {
                case "cs":
                case "pl":
                    StringEncodingController.SetEncoding(StringEncoding.CentralEurope);
                    break;
                case "ru":
                    StringEncodingController.SetEncoding(StringEncoding.EastEurope);
                    break;
                case "de":
                case "en":
                case "es":
                case "fr":
                case "it":
                default:
                    StringEncodingController.SetEncoding(StringEncoding.WestEurope);
                    break;
            }
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
