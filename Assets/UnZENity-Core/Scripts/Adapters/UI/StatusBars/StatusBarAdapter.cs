using System;
using System.Collections;
using GUZ.Core.Services.Meshes;
using GUZ.Core.Services.Player;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.Core.Adapters.UI.StatusBars
{
    public class StatusBarAdapter : MonoBehaviour
    {
        [SerializeField] private StatusType _statusType;
        [SerializeField] private Image _background;
        [SerializeField] private Image _statusValue;

        [Inject] private readonly TextureService _textureService;
        [Inject] private readonly PlayerService _playerService;

        private enum StatusType
        {
            Health,
            Mana,
            Misc
        }

        private void Start()
        {
            DisableBar();
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(StartDelayed);
        }

        private void StartDelayed()
        {
            _background.material = _textureService.StatusBarBackgroundMaterial;

            _statusValue.fillAmount = 1f;
            _statusValue.material = _statusType switch
            {
                StatusType.Health => _textureService.StatusBarHealthMaterial,
                StatusType.Mana => _textureService.StatusBarManaMaterial,
                StatusType.Misc => _textureService.StatusBarMiscMaterial,
                _ => throw new ArgumentOutOfRangeException()
            };

            switch (_statusType)
            {
                case StatusType.Health:
                    break;
                case StatusType.Mana:
                    break;
                case StatusType.Misc:
                    StartCoroutine(HandleDiveValue());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator HandleDiveValue()
        {
            while (true)
            {
                if (!_playerService.IsDiving)
                {
                    DisableBar();
                }
                else
                {
                    _statusValue.fillAmount = _playerService.CurrentAir / _playerService.MaxAir;
                    EnableBar();
                }

                yield return null;
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void DisableBar()
        {
            _background.enabled = false;
            _statusValue.enabled = false;
        }

        public void EnableBar()
        {
            _background.enabled = true;
            _statusValue.enabled = true;
        }
    }
}
