using System;
using System.Collections;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace GUZ.XRIT.Manager
{
    [Obsolete("Not yet migrated to new HVR/XRIT logic. Can be removed once the menus are migrated to HVR.")]
    public class XRITControllerManager : SingletonBehaviour<XRITControllerManager>
    {
        [FormerlySerializedAs("raycastLeft")] public GameObject RaycastLeft;
        [FormerlySerializedAs("raycastRight")] public GameObject RaycastRight;
        [FormerlySerializedAs("directLeft")] public GameObject DirectLeft;
        [FormerlySerializedAs("directRight")] public GameObject DirectRight;
        public GameObject MenuGameObject;
        
        private InputAction _leftPrimaryButtonAction;
        private InputAction _leftSecondaryButtonAction;

        private InputAction _rightPrimaryButtonAction;
        private InputAction _rightSecondaryButtonAction;

        public GameObject MapObject;
        [FormerlySerializedAs("maprollspeed")] public float Maprollspeed;

        [FormerlySerializedAs("maprolloffset")]
        public float Maprolloffset;

        private Animator _maproll;
        private AudioSource _mapaudio;
        private AudioClip _scrollsound;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            _maproll = MapObject.gameObject.GetComponent<Animator>();
            _mapaudio = MapObject.gameObject.GetComponent<AudioSource>();
            _scrollsound = VobHelper.GetSoundClip("SCROLLROLL.WAV");
            MapObject.SetActive(false);
            _maproll.enabled = false;

            _leftPrimaryButtonAction = new InputAction("primaryButton", binding: "<XRController>{LeftHand}/primaryButton");
            _leftSecondaryButtonAction =
                new InputAction("secondaryButton", binding: "<XRController>{LeftHand}/secondaryButton");

            _leftPrimaryButtonAction.started += ctx => ShowRayCasts();
            _leftPrimaryButtonAction.canceled += ctx => HideRayCasts();

            _leftPrimaryButtonAction.Enable();
            _leftSecondaryButtonAction.Enable();

            _rightPrimaryButtonAction =
                new InputAction("primaryButton", binding: "<XRController>{RightHand}/primaryButton");
            _rightSecondaryButtonAction =
                new InputAction("secondaryButton", binding: "<XRController>{RightHand}/secondaryButton");

            _rightPrimaryButtonAction.started += ctx => ShowMap();
            _rightSecondaryButtonAction.started += ctx => ShowMainMenu();

            _rightPrimaryButtonAction.Enable();
            _rightSecondaryButtonAction.Enable();
        }

        private void OnDestroy()
        {
            _leftPrimaryButtonAction?.Disable();
            _leftSecondaryButtonAction?.Disable();

            _rightPrimaryButtonAction?.Disable();
            _rightSecondaryButtonAction?.Disable();
        }

        public void ShowRayCasts()
        {
            RaycastLeft.SetActive(true);
            RaycastRight.SetActive(true);
            DirectLeft.SetActive(false);
            DirectRight.SetActive(false);
        }

        public void HideRayCasts()
        {
            RaycastLeft.SetActive(false);
            RaycastRight.SetActive(false);
            DirectLeft.SetActive(true);
            DirectRight.SetActive(true);
        }

        public void ShowMainMenu()
        {
            if (!MenuGameObject.activeSelf)
            {
                MenuGameObject.SetActive(true);
            }
            else
            {
                MenuGameObject.SetActive(false);
            }
        }

        public void ShowMap()
        {
            if (!MapObject.activeSelf)
            {
                StartCoroutine(UnrollMap());
            }
            else
            {
                StartCoroutine(RollupMap());
            }
        }

        public IEnumerator UnrollMap()
        {
            MapObject.SetActive(true);
            _maproll.enabled = true;
            _maproll.speed = Maprollspeed;
            _maproll.Play("Unroll", -1, 0.0f);
            _mapaudio.PlayOneShot(_scrollsound);
            yield return new WaitForSeconds(_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length / Maprollspeed *
                                            (_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length - Maprolloffset) /
                                            _maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length);
            _maproll.speed = 0f;
        }

        public IEnumerator RollupMap()
        {
            _maproll.speed = Maprollspeed;
            _maproll.Play("Roll", -1,
                1 - (_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length - Maprolloffset) /
                _maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length);
            _mapaudio.PlayOneShot(_scrollsound);
            yield return new WaitForSeconds(_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length / Maprollspeed *
                                            (_maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length - Maprolloffset) /
                                            _maproll.GetCurrentAnimatorClipInfo(0)[0].clip.length);
            _maproll.speed = 0f;
            MapObject.SetActive(false);
        }
    }
}

