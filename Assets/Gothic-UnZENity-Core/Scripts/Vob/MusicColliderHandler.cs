using GUZ.Core.Debugging;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using UnityEngine;

namespace GUZ.Core.Vob
{
    public class MusicCollisionHandler : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!FeatureFlags.I.enableMusic)
                return;

            if (!other.CompareTag(Constants.PlayerTag))
                return;
            
            // FIXME - We need to load the currently active music when spawned. Currently we need to walk 1cm to trigger collider.
            MusicManager.I.AddMusicZone(gameObject);
            MusicManager.I.Play(MusicManager.SegmentTags.Std);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!FeatureFlags.I.enableMusic)
                return;
            
            if (!other.CompareTag(Constants.PlayerTag))
                return;

            MusicManager.I.RemoveMusicZone(gameObject);
            MusicManager.I.Play(MusicManager.SegmentTags.Std);
        }
    }
}
