using GUZ.Core.Manager;
using UnityEngine;

namespace GUZ.Core.Vob
{
    public class ChangeLevelTriggerHandler : MonoBehaviour
    {
        public string levelName;
        public string startVob;
        
        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            GlobalEventDispatcher.LevelChangeTriggered.Invoke(levelName, startVob.Trim());
        }
    }
}
