using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.Core.Vob
{
    public class MusicCollisionHandler : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(Constants.PlayerTag))
            {
                return;
            }

            // FIXME - We need to load the currently active music when spawned. Currently we need to walk 1cm to trigger collider.
            GlobalEventDispatcher.MusicZoneEntered.Invoke(gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(Constants.PlayerTag))
            {
                return;
            }

            GlobalEventDispatcher.MusicZoneExited.Invoke(gameObject);
        }
    }
}
