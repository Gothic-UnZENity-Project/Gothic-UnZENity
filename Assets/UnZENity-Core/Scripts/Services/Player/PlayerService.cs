using UnityEngine;

namespace GUZ.Core.Services.Player
{
    public class PlayerService
    {
        public Vector3 HeroSpawnPosition;
        public Quaternion HeroSpawnRotation;

        public string LastLevelChangeTriggerVobName;

        public void ResetSpawn()
        {
            HeroSpawnPosition = default;
            HeroSpawnRotation = default;
        }
    }
}
