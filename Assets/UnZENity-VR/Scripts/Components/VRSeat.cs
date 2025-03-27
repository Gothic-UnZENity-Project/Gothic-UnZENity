#if GUZ_HVR_INSTALLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Player.Camera;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Utils;
using UnityEngine;

namespace GUZ.VR.Components
{
    public class VRSeat : MonoBehaviour
    {
        //this will enable player to sit on benches/chairs etc
        private Vector3 _posOffset = new(0, -0.75f, 0.9f);
        private Vector3 _eulerOffset = new(0, 180, 0);
        private const float _cameraFadeDuration = 0.15f;
        private const float _sittingCooldown = 0.5f;

        private bool _isPlayerSeated;

        //when NPC sits down this should be changed to true and when NPC stands up change it back to false
        private bool _isNpcSeated;

        private List<Transform> _snapPoints = new();
        private Transform _currentSnapPoint;

        private CameraFade _cachedCameraFade;
        private bool _canPlayerSit = true;

        private void GetSnapPoints()
        {
            var zsPosParent = transform.FindChildRecursive("ZS_POS0").parent;
            //find children that contain "ZS_POS" in object name and add to list of snap points
            for (var i = 0; i < zsPosParent.childCount; i++)
            {
                var child = zsPosParent.GetChild(i);
                if (child.name.ToLower().Contains("zs_pos"))
                {
                    //add snap point
                    _snapPoints.Add(child);
                }
            }
        }

        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (_snapPoints.IsEmpty())
            {
                GetSnapPoints();
            }
            if (_isNpcSeated || !_canPlayerSit)
            {
                return; // stop player from sitting if an NPC is already sitting there or if its cooling down
            }

            //get player object
            _canPlayerSit = false; // disable this function to cooldown

            GameObject player = GameContext.InteractionAdapter.GetCurrentPlayerController();

            //handle sitting/standing
            _isPlayerSeated = !_isPlayerSeated;
            if (_isPlayerSeated)
            {
                StartCoroutine(SitDown(player));
            }
            else
            {
                StartCoroutine(StandUp(player));
            }

            Invoke(nameof(EnableSitting), _sittingCooldown);
        }

        private void EnableSitting()
        {
            _canPlayerSit = true;
        }

        private IEnumerator SitDown(GameObject player)
        {
            //lock input
            GameContext.InteractionAdapter.LockPlayerInPlace();

            //get snap point
            _currentSnapPoint = GetNearestSnapPoint(player.transform.position);

            //fade camera out
            _cachedCameraFade = player.GetComponentInChildren<CameraFade>();
            _cachedCameraFade.Fade(_cameraFadeDuration, 1);
            yield return new WaitForSeconds(_cameraFadeDuration);

            //set position and rotation
            player.transform.position = _currentSnapPoint.position + _currentSnapPoint.TransformDirection(_posOffset);
            player.transform.eulerAngles = _currentSnapPoint.eulerAngles + _eulerOffset;

            //fade camera in
            _cachedCameraFade.Fade(_cameraFadeDuration, 0);
        }

        private IEnumerator StandUp(GameObject player)
        {
            //fade camera out
            _cachedCameraFade.Fade(_cameraFadeDuration, 1);
            yield return new WaitForSeconds(_cameraFadeDuration);
            //move player forward
            player.transform.position += player.transform.TransformDirection(new Vector3(0, 0.5f, 1f));
            // player.transform.eulerAngles = cachedEulers;
            //unlock move an turn input
            GameContext.InteractionAdapter.UnlockPlayer();
            //fade camera in
            _cachedCameraFade.Fade(_cameraFadeDuration, 0);
            _cachedCameraFade = null;
        }

        private Transform GetNearestSnapPoint(Vector3 pos)
        {
            // Find the closest Snap Point for player to sit.
            return _snapPoints
                .OrderBy(point => Vector3.Distance(pos, point.position))
                .First();
        }
    }
}
#endif
