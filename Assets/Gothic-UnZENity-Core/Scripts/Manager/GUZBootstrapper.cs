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
            AssetCache.Dispose();
            TextureCache.Dispose();
            LookupCache.Dispose();
            PrefabCache.Dispose();
            MorphMeshCache.Dispose();
        }
        
        public static void BootGothicUnZENity(string g1Dir)
        {
            var watch = Stopwatch.StartNew();

            GUZContext.SetContext(FeatureFlags.I.gameControls);

            MountVfs(g1Dir);
            SetLanguage();
            LoadGothicVm(g1Dir);
            LoadDialogs();
            LoadSfxVm(g1Dir);
            LoadPfxVm(g1Dir);
            LoadMusicVm(g1Dir);
            LoadMusic();
            LoadFonts();
            
            watch.Stop();
            Debug.Log($"Time spent for Bootstrapping ZenKit: {watch.Elapsed}");

            GUZEvents.ZenKitBootstrapped.Invoke();
        }

        /// <summary>
        /// Holy grail of everything! If this pointer is zero, we have nothing but a plain empty wormhole.
        /// </summary>
        public static void MountVfs(string g1Dir)
        {
            GameData.Vfs = new Vfs();

            // FIXME - We currently don't load from within _WORK directory which is required for e.g. mods who use it.
            var fullPath = Path.GetFullPath(Path.Join(g1Dir, "Data"));

            var vfsPaths = Directory.GetFiles(fullPath, "*.VDF", SearchOption.AllDirectories);

            foreach (var path in vfsPaths)
            {
                GameData.Vfs.MountDisk(path, VfsOverwriteBehavior.Older);
            }
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
            var fullPath = Path.GetFullPath(Path.Join(g1Dir, "/_work/DATA/scripts/_compiled/GOTHIC.DAT"));
            GameData.GothicVm = new DaedalusVm(fullPath);
            
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
            var fullPath = Path.GetFullPath(Path.Join(g1Dir, "/_work/DATA/scripts/_compiled/SFX.DAT"));
            GameData.SfxVm = new DaedalusVm(fullPath);
        }

        private static void LoadPfxVm(string g1Dir)
        {
            var fullPath = Path.GetFullPath(Path.Join(g1Dir, "/_work/DATA/scripts/_compiled/PARTICLEFX.DAT"));
            GameData.PfxVm = new DaedalusVm(fullPath);
        }

        private static void LoadMusicVm(string g1Dir)
        {
            var fullPath = Path.GetFullPath(Path.Join(g1Dir, "/_work/DATA/scripts/_compiled/MUSIC.DAT"));
            GameData.MusicVm = new DaedalusVm(fullPath);
        }

        private static void LoadMusic()
        {
            MusicManager.Initialize();
        }

        private static void LoadFonts()
        {
            FontManager.I.Create();
        }
    }
}
