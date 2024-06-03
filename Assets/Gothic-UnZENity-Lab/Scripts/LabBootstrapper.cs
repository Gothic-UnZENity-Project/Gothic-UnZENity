using GUZ.Core.Caches;
using GUZ.Core.Globals;
using GUZ.Core.Lab.Handler;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Settings;
using UnityEngine;

namespace GUZ.Core.GothicVR_Lab.Scripts
{
    public class LabBootstrapper : MonoBehaviour
    {
        public LabMusicHandler labMusicHandler;
        public LabNpcDialogHandler npcDialogHandler;
        public LabLockableLabHandler lockableLabHandler;
        public LabLadderLabHandler ladderLabHandler;
        public LabVobHandAttachPointsLabHandler vobHandAttachPointsLabHandler;
        public LabNpcAnimationHandler labNpcAnimationHandler;

        private bool _isBooted;
        /// <summary>
        /// It's easiest to wait for Start() to initialize all the MonoBehaviours first.
        /// </summary>
        private void Update()
        {
            if (_isBooted)
                return;
            _isBooted = true;
            
            GvrBootstrapper.BootGothicVR(SettingsManager.GameSettings.GothicIPath);

            BootLab();

            labNpcAnimationHandler.Bootstrap();
            labMusicHandler.Bootstrap();
            npcDialogHandler.Bootstrap();
            lockableLabHandler.Bootstrap();
            ladderLabHandler.Bootstrap();
            vobHandAttachPointsLabHandler.Bootstrap();
        }

        private void BootLab()
        {
            NpcHelper.CacheHero();
        }

        private void OnDestroy()
        {
            GameData.Dispose();
            AssetCache.Dispose();
            TextureCache.Dispose();
            LookupCache.Dispose();
            PrefabCache.Dispose();
            MorphMeshCache.Dispose();
        }
    }
}
