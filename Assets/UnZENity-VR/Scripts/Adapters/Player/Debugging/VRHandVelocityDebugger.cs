using System;
using GUZ.VR.Adapters.HVROverrides;
using UnityEngine;

namespace GUZ.VR.Adapters.Player.Debugging
{
    public class VRHandVelocityDebugger : MonoBehaviour
    {
        [SerializeField] private VRPlayerController _playerController;
        [SerializeField] private VRPlayerInputs _playerInputs;
        [SerializeField] private GameObject _leftHandModel;
        [SerializeField] private GameObject _rightHandModel;
        [SerializeField] private Rigidbody _leftHandRigidbody;
        [SerializeField] private Rigidbody _rightHandRigidbody;


        private LineRenderer _lineRendererLeft;
        private LineRenderer _lineRendererRight;

        private void Start()
        {
            // Add a LineRenderer component
            _lineRendererLeft = _leftHandModel.AddComponent<LineRenderer>();
            _lineRendererRight = _rightHandModel.AddComponent<LineRenderer>();

            // Set the material
            _lineRendererLeft.material = new Material(Shader.Find("Sprites/Default"));
            _lineRendererRight.material = new Material(Shader.Find("Sprites/Default"));

            // Set the width
            _lineRendererLeft.startWidth = 0.1f;
            _lineRendererLeft.endWidth = 0.1f;
            _lineRendererRight.startWidth = 0.1f;
            _lineRendererRight.endWidth = 0.1f;

            // Set the color
            _lineRendererLeft.startColor = Color.red;
            _lineRendererLeft.endColor = Color.red;
            _lineRendererRight.startColor = Color.red;
            _lineRendererRight.endColor = Color.red;
        }

        private void FixedUpdate()
        {
            _lineRendererRight.SetPosition(0, _rightHandModel.transform.position);
            _lineRendererRight.SetPosition(1, _rightHandModel.transform.position + _playerInputs.RightController.Velocity * 10);
        }
    }
}
