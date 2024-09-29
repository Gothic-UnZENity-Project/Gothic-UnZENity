using UnityEngine;

namespace GUZ.Core.Manager
{
    public class PlayerManager
    {
        public Vector3 HeroSpawnPosition;
        public Quaternion HeroSpawnRotation;

        public PlayerManager(GameConfiguration config)
        {
            // Nothing to do for now. Might be needed later.
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
