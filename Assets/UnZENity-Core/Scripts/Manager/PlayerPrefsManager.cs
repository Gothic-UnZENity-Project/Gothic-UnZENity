using System;
using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public class PlayerPrefsManager
    {
        // Movement - Direction
        private const int _directionModeCamera = 0;
        private const int _directionModeLeftController = 1;
        private const int _directionModeRightController = 2;
        
        // Movement - Rotation
        private const int _rotationTypeSmooth = 0;
        private const int _rotationTypeSnap = 1;
        private const int _defaultSmoothRotationSpeed = 90; // Default taken from HVRPlayerController.SmoothTurnSpeed
        private const int _defaultSnapRotationAmount = 45; // Default taken from HVRPlayerController.SnapAmount
        
        // Gameplay
        private const bool _defaultItemCollisionWhileDragged = true;
        
        // Audio
        private const float _defaultMusicVolume = 0.2f;
        
        
        /**
         * Movement settings
         */

        public static int DirectionMode
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefDirectionMode, _directionModeCamera);
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefDirectionMode, value);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefDirectionMode, DirectionMode);
            }
        }

        public static bool MovementDirectionCamera
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefDirectionMode, _directionModeCamera) == _directionModeCamera;
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefDirectionMode, value ? _directionModeCamera : -1);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefDirectionMode, MovementDirectionCamera);
            }
        }

        public static bool MovementDirectionLeftController
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefDirectionMode, _directionModeCamera) == _directionModeLeftController;
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefDirectionMode, value ? _directionModeLeftController : -1);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefDirectionMode, MovementDirectionLeftController);
            }
        }

        public static bool MovementDirectionRightController
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefDirectionMode, _directionModeCamera) == _directionModeRightController;
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefDirectionMode, value ? _directionModeRightController : -1);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefDirectionMode, MovementDirectionRightController);
            }
        }


        /**
         * Turn settings
         */
        
        public static int RotationType
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefRotationType, _rotationTypeSnap);
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefRotationType, value);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefRotationType, RotationType);
            }
        }

        public static bool SmoothRotation
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefRotationType, _rotationTypeSnap) == _rotationTypeSmooth;
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefRotationType, value ? _rotationTypeSmooth : _rotationTypeSnap);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefRotationType, SmoothRotation);
            }
        }

        public static bool SnapRotation
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefRotationType, _rotationTypeSnap) == _rotationTypeSnap;
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefRotationType, value ? _rotationTypeSnap : _rotationTypeSmooth);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefRotationType, SnapRotation);
            }
        }

        public static int SmoothRotationSpeed
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefSmoothRotationSpeed, _defaultSmoothRotationSpeed);
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefSmoothRotationSpeed, value);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefSmoothRotationSpeed, SmoothRotationSpeed);
            }
        }

        public static int SnapRotationAmount
        {
            get => PlayerPrefs.GetInt(Constants.PlayerPrefSnapRotationAmount, _defaultSnapRotationAmount);
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefSnapRotationAmount, value);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefSnapRotationAmount, SnapRotationAmount);
            }
        }

        public static bool ItemCollisionWhileDragged
        {
            get => Convert.ToBoolean(PlayerPrefs.GetInt(Constants.PlayerPrefItemCollisionWhileDragged, Convert.ToInt32(_defaultItemCollisionWhileDragged)));
            set
            {
                PlayerPrefs.SetInt(Constants.PlayerPrefItemCollisionWhileDragged, Convert.ToInt32(value));
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefItemCollisionWhileDragged, ItemCollisionWhileDragged);
            }
        }
        
        
        /**
         * Audio settings
         */
         
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(Constants.PlayerPrefMusicVolume, _defaultMusicVolume);
            set
            {
                PlayerPrefs.SetFloat(Constants.PlayerPrefMusicVolume, value);
                GlobalEventDispatcher.PlayerPrefUpdated.Invoke(Constants.PlayerPrefMusicVolume, MusicVolume);
            }
        }
    }
}
