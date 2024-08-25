using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Player.Camera;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GUZ.Core.Player.Interactive
{
    public class Seat : MonoBehaviour
    {
        //this will enable player to sit on benches/chairs etc
        private Vector3 _posOffset = new(0, -0.75f, 0.9f);
        private Vector3 _eulerOffset = new(0, 180, 0);
        private const float _cameraFadeDuration = 0.15f;
        private const float _sittingCooldown = 0.5f;

        private XRGrabInteractable _interactable;
        private bool _isPlayerSeated;

         //when NPC sits down this should be changed to true and when NPC stands up change it back to false
        private bool _isNpcSeated;

        private List<Transform> _snapPoints = new();
        private Transform _currentSnapPoint;

        private Vector3 _cachedPos, _cachedEulers;
        private GameObject _cachedLocomotion;
        private CameraFade _cachedCameraFade;
        private bool _canPlayerSit = true;

        private void Start()
        {
            // FIXME - Previously used another layer. Still needed?
            // gameObject.layer = _Constants.InteractiveLayer;

            //get snap points
            GetSnapPoints();
            //get interactable
            _interactable = GetComponent<XRGrabInteractable>();
            //add ToggleSitting listener to SelectEntered
            _interactable.selectEntered.AddListener(ToggleSitting);
        }

        private void GetSnapPoints()
        {
            //find children that contain "ZS_POS" in object name and add to list of snap points
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.ToLower().Contains("zs_pos"))
                {
                    //add snap point
                    _snapPoints.Add(child);
                }
            }
        }

        public void ToggleSitting(SelectEnterEventArgs args)
        {
            if (_isNpcSeated || !_canPlayerSit)
            {
                return; // stop player from sitting if an NPC is already sitting there or if its cooling down
            }

            //get player object
            _canPlayerSit = false; //disable this function to cooldown
            var player = args.interactorObject.transform.GetComponentInParent<XROrigin>().gameObject;
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

            Invoke("EnableSitting", _sittingCooldown);
        }

        private void EnableSitting()
        {
            _canPlayerSit = true;
        }

        private IEnumerator SitDown(GameObject player)
        {
            //cache player pos and eulers
            _cachedPos = player.transform.position;
            _cachedEulers = player.transform.eulerAngles;

            //lock input
            _cachedLocomotion = player.GetComponentInChildren<LocomotionSystem>().gameObject;
            _cachedLocomotion.SetActive(false);

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
            _cachedLocomotion.SetActive(true);
            //fade camera in
            _cachedCameraFade.Fade(_cameraFadeDuration, 0);
            //clear cache
            _cachedPos = Vector3.zero;
            _cachedEulers = Vector3.zero;
            _cachedLocomotion = null;
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
