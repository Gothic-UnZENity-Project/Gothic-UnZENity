using UnityEngine;

namespace GUZ.Core.Manager
{
    public class PlayerManager
    {
        public Vector3 HeroSpawnPosition;
        public Quaternion HeroSpawnRotation;

        public string LastLevelChangeTriggerVobName;

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
