using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public class PlayerPrefsManager
    {
        private const int _movementDirectionCamera = 0;
        private const int _movementDirectionLeftController = 1;
        private const int _movementDirectionRightController = 2;
        
        private const int _turnTypeSnap = 0;
        private const int _turnTypeSmooth = 1;
        private const int _defaultSnapTurnAngle = 10;
        private const int _defaultSmoothTurnSpeed = 2;

        /**
         * Movement settings
         */

        public static int MovementType
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefMovementDirection, _movementDirectionCamera);
            set => PlayerPrefs.SetInt(Constants.PlayerPrefMovementDirection, value);
        }    
        
        public static bool MovementDirectionCamera
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefMovementDirection, _movementDirectionCamera) == _movementDirectionCamera;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefMovementDirection, value ? _movementDirectionCamera : -1);
        }

        public static bool MovementDirectionLeftController
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefMovementDirection, _movementDirectionCamera) == _movementDirectionLeftController;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefMovementDirection, value ? _movementDirectionLeftController : -1);
        }
        
        public static bool MovementDirectionRightController
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefMovementDirection, _movementDirectionCamera) == _movementDirectionRightController;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefMovementDirection, value ? _movementDirectionRightController : -1);
        }
        
        
        /**
         * Turn settings
         */
        
        public static int TurnType
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefTurnType, _turnTypeSnap);
            set => PlayerPrefs.SetInt(Constants.PlayerPrefTurnType, value);
        }
        
        public static bool SnapTurn
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefTurnType, _turnTypeSnap) == 0;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefTurnType, value ? _turnTypeSnap : _turnTypeSmooth);
        }
        
        public static bool SmoothTurn
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefTurnType, _turnTypeSnap) == 1;
            set => PlayerPrefs.SetInt(Constants.PlayerPrefTurnType, value ? _turnTypeSmooth : _turnTypeSnap);
        }

        public static int SnapTurnAngle
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefSnapTurnAngle, _defaultSnapTurnAngle);
            set => PlayerPrefs.SetInt(Constants.PlayerPrefSnapTurnAngle, value);
        }

        public static int SmoothTurnSpeed
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefSmoothTurnSpeed, _defaultSmoothTurnSpeed);
            set => PlayerPrefs.SetInt(Constants.PlayerPrefSmoothTurnSpeed, value);
        }
    }
}
