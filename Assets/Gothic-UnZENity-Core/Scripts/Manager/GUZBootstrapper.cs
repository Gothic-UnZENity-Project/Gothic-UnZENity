using System.Diagnostics;
using System.IO;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Debugging;
using GUZ.Core.Globals;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using GVR.Core;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager
{
    public class GUZBootstrapper : SingletonBehaviour<GUZBootstrapper>
    {
        private bool isBootstrapped;
        public GameObject invalidInstallationDirMessage;

        private void OnApplicationQuit()
        {
            GameData.Dispose();
            VmInstanceManager.Dispose();
            TextureCache.Dispose();
            LookupCache.Dispose();
            MorphMeshCache.Dispose();
        }
        
        public static void BootGothicUnZENity(string g1Dir)
        {
            var watch = Stopwatch.StartNew();

            GUZContext.SetContext(FeatureFlags.I.gameControls);
            
            SetLanguage();
            LoadGothicVm(g1Dir);
            LoadDialogs();
            LoadSfxVm(g1Dir);
            LoadPfxVm(g1Dir);
            LoadFonts();
            
            watch.Stop();
            Debug.Log($"Time spent for Bootstrapping ZenKit: {watch.Elapsed}");

            GlobalEventDispatcher.ZenKitBootstrapped.Invoke();
        }

        public static void SetLanguage()
        {
            var g1Language = SettingsManager.GameSettings.GothicILanguage;

            switch (g1Language?.Trim().ToLower())
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
            FontManager.I.Create();
        }
    }
}