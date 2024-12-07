using UnityEngine;

namespace GUZ.Core.Manager
{
    public class PlayerManager
    {
        public Vector3 HeroSpawnPosition;
        public Quaternion HeroSpawnRotation;

        public PlayerManager(GameConfiguration config)
        {
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(SetQuality);
        }

        private void SetQuality()
        {
            Camera.main!.farClipPlane = GameGlobals.Config.RenderDistance;
        }

        public void Init()
        {
            // Nothing to do for now. Might be needed later.
        }

        public void ResetSpawn()
        {
            HeroSpawnPosition = default;
            HeroSpawnRotation = default;
        }
    }
}
