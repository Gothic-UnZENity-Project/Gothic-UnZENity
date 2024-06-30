using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace GUZ.Core.Player.Climb
{
    public class RayTeleportClimb : MonoBehaviour
    {
        [FormerlySerializedAs("lineVisual")] [SerializeField]
        private XRInteractorLineVisual _lineVisual;

        [FormerlySerializedAs("interactor")] [SerializeField]
        private XRBaseInteractor _interactor;

        [FormerlySerializedAs("player")] [SerializeField]
        private GameObject _player;

        [FormerlySerializedAs("sprite")] [SerializeField]
        private Sprite _sprite;

        private GameObject _teleportIndicatorReticle;
        private Image _teleportIndicatorReticleImage;

        private bool _isHittingObject;
        private GameObject _zsPos0Go;
        private GameObject _zsPos1Go;
        private float _hitTime;
        private float _teleportDelay = 1f; // Adjust the delay duration as needed

        private string _zsPos0 = "ZS_POS0";
        private string _zsPos1 = "ZS_POS1";

        private void Start()
        {
            CreateReticle();
            _interactor.selectEntered.AddListener(OnRaycastHit);
            _interactor.selectExited.AddListener(OnRaycastExit);
        }

        private void OnRaycastHit(SelectEnterEventArgs args)
        {
            // Check if the interactable GameObject has the tag "Climbable"
            if (args.interactableObject != null &&
                args.interactableObject.transform.CompareTag(Constants.ClimbableTag))
            {
                // Show a message in the logs
                var hitObject = args.interactableObject.transform.gameObject;

                _zsPos0Go = hitObject.FindChildRecursively(_zsPos0);
                _zsPos1Go = hitObject.FindChildRecursively(_zsPos1);

                // Get the zs_pos0 and zs_pos1 positions
                var zsPos0Position = _zsPos0Go.transform.position;
                var zsPos1Position = _zsPos1Go.transform.position;

                var playerPosition = _player.transform.position;

                var yDifferenceToZsPos0 = Mathf.Abs(playerPosition.y - zsPos0Position.y);
                var yDifferenceToZsPos1 = Mathf.Abs(playerPosition.y - zsPos1Position.y);

                _teleportIndicatorReticle.SetActive(true);

                // Teleport the player to the closer zs_pos position based on y-level
                if (yDifferenceToZsPos0 < yDifferenceToZsPos1)
                {
                    _teleportIndicatorReticle.SetParent(_zsPos0Go, true, true);
                    _teleportIndicatorReticleImage.rectTransform.localRotation =
                        Quaternion.AngleAxis(0, Vector3.forward);
                }
                else
                {
                    _teleportIndicatorReticle.SetParent(_zsPos1Go, true, true);
                    _teleportIndicatorReticleImage.rectTransform.localRotation =
                        Quaternion.AngleAxis(180, Vector3.forward);
                }

                _isHittingObject = true;

                // Record the hit time
                _hitTime = Time.time;
            }
        }

        private void OnRaycastExit(SelectExitEventArgs args)
        {
            _isHittingObject = false;
            _teleportIndicatorReticle.transform.parent = null;
            _teleportIndicatorReticleImage.fillAmount = 0;
            _teleportIndicatorReticle.SetActive(false);
        }

        private void Update()
        {
            if (_isHittingObject)
            {
                _teleportIndicatorReticleImage.fillAmount = (Time.time - _hitTime) / _teleportDelay;
                // Check if the delay duration has passed since the hit
                if (Time.time - _hitTime >= _teleportDelay)
                {
                    PerformTeleport();
                }
            }
        }

        private void PerformTeleport()
        {
            // Get the player's position
            var playerPosition = _player.transform.position;

            var zsPos0Position = _zsPos0Go.transform.position;
            var zsPos1Position = _zsPos1Go.transform.position;

            var yDifferenceToZsPos0 = Mathf.Abs(playerPosition.y - zsPos0Position.y);
            var yDifferenceToZsPos1 = Mathf.Abs(playerPosition.y - zsPos1Position.y);

            // Teleport the player to the closer zs_pos position based on y-level
            if (yDifferenceToZsPos0 < yDifferenceToZsPos1)
            {
                TeleportPlayer(zsPos1Position);
            }
            else
            {
                TeleportPlayer(zsPos0Position);
            }

            // Reset the state
            _isHittingObject = false;

            // Deactivate the teleport ray
            if (_interactor.gameObject.name.Contains("Teleport"))
            {
                _interactor.enabled = false;
            }
        }

        private void TeleportPlayer(Vector3 targetPosition)
        {
            // Teleport the player to the target position
            _player.transform.position = targetPosition;
        }

        private void CreateReticle()
        {
            _teleportIndicatorReticle = new GameObject("Reticle");

            var canvas = new GameObject("Canvas");
            canvas.SetParent(_teleportIndicatorReticle);
            canvas.AddComponent<Canvas>();
            var image = new GameObject("Image");
            image.SetParent(canvas);


            _teleportIndicatorReticleImage = image.AddComponent<Image>();

            _teleportIndicatorReticleImage.sprite = _sprite;
            _teleportIndicatorReticleImage.rectTransform.sizeDelta = new Vector2(1f, 1f);
            _teleportIndicatorReticleImage.material = GameGlobals.Textures.ArrowMaterial;
            _teleportIndicatorReticleImage.type = Image.Type.Filled;
            _teleportIndicatorReticleImage.fillMethod = Image.FillMethod.Vertical;
            Destroy(_teleportIndicatorReticle.GetComponent<Collider>());

            _teleportIndicatorReticle.SetActive(false);
        }
    }
}
