using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public class PlayerPrefsManager
    {
        private const int _directionModeCamera = 0;
        private const int _directionModeLeftController = 1;
        private const int _directionModeRightController = 2;
        
        private const int _rotationTypeSmooth = 0;
        private const int _rotationTypeSnap = 1;
        private const int _defaultSmoothRotationSpeed = 2;
        private const int _defaultSnapRotationAmount = 10;

        /**
         * Movement settings
         */

        public static int DirectionMode
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefDirectionMode, _directionModeCamera);
            set => PlayerPrefs.SetInt(Constants.PlayerPrefDirectionMode, value);
        }    
        
        public static bool MovementDirectionCamera
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefDirectionMode, _directionModeCamera) == _directionModeCamera;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefDirectionMode, value ? _directionModeCamera : -1);
        }

        public static bool MovementDirectionLeftController
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefDirectionMode, _directionModeCamera) == _directionModeLeftController;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefDirectionMode, value ? _directionModeLeftController : -1);
        }
        
        public static bool MovementDirectionRightController
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefDirectionMode, _directionModeCamera) == _directionModeRightController;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefDirectionMode, value ? _directionModeRightController : -1);
        }
        
        
        /**
         * Turn settings
         */
        
        public static int RotationType
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefRotationType, _rotationTypeSnap);
            set => PlayerPrefs.SetInt(Constants.PlayerPrefRotationType, value);
        }
        
        
        public static bool SmoothRotation
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefRotationType, _rotationTypeSnap) == _rotationTypeSmooth;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefRotationType, value ? _rotationTypeSmooth : _rotationTypeSnap);
        }
        
        public static bool SnapRotation
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefRotationType, _rotationTypeSnap) == _rotationTypeSnap;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefRotationType, value ? _rotationTypeSnap : _rotationTypeSmooth);
        }
        
        public static int SmoothRotationSpeed
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefSmoothRotationSpeed, _defaultSmoothRotationSpeed);
            set => PlayerPrefs.SetInt(Constants.PlayerPrefSmoothRotationSpeed, value);
        }

        public static int SnapRotationAmount
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefSnapRotationAmount, _defaultSnapRotationAmount);
            set => PlayerPrefs.SetInt(Constants.PlayerPrefSnapRotationAmount, value);
        }
    }
}
