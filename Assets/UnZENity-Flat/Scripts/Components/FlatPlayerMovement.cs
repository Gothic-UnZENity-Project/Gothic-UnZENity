using UnityEngine;
using UnityEngine.InputSystem;

namespace GUZ.Flat.Components
{
    public class FlatPlayerMovement : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        
        private Vector3 _playerVelocity;
        private bool _groundedPlayer;
        private float _playerSpeed = 2.0f;
        private float _jumpHeight = 1.0f;
        private float _gravityValue = -9.81f;
        
        private Vector3 _mouseDelta => Mouse.current.delta.ReadValue();
        
        
        void Update()
        {
            _groundedPlayer = _characterController.isGrounded;
            if (_groundedPlayer && _playerVelocity.y < 0)
            {
                _playerVelocity.y = 0f;
            }
            
            var rot = transform.TransformDirection(Vector3.left);
            
            // var move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            // _characterController.Move(rot * Time.deltaTime * _playerSpeed);

            if (IsPressed(Key.W))
            {
                gameObject.transform.forward = default;
            }

            if (IsPressed(Key.A))
            {
                gameObject.transform.forward = default; // aka -1 forward
            }

            // Changes the height position of the player..
            if (Keyboard.current[Key.Space].isPressed && _groundedPlayer)
            {
                _playerVelocity.y += Mathf.Sqrt(_jumpHeight * -3.0f * _gravityValue);
            }

            _playerVelocity.y += _gravityValue * Time.deltaTime;
            _characterController.Move(_playerVelocity * Time.deltaTime);
        }

        private bool IsPressed(Key key)
        {
            return Keyboard.current[key].isPressed;
        }
    }
}
