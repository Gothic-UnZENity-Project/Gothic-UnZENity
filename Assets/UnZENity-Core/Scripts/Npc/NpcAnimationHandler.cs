using System;
using System.Collections;
using GUZ.Core._Npc2;
using GUZ.Core.Vm;
using JetBrains.Annotations;
using MyBox;
using UnityEngine;

namespace GUZ.Core.Npc
{
    public class NpcAnimationHandler : BasePlayerBehaviour
    {
        private bool _isAnimationPlaying;
        private float _blendOutTime;
        private string _nextAnimation;

        protected override void Awake()
        {
            base.Awake();

            // Cached object which will be used later.
            NpcData.PrefabProps.AnimationHandler = this;
            NpcData.PrefabProps.Animation = GetComponent<Animation>();
        }

        private void Start()
        {
            StartCoroutine(BlendOutCoroutine());
        }

        public void StartLookAt(Transform destinationTransform)
        {
            _doLookAtNpc = true;
            _destTransform = destinationTransform;
        }

        private bool _doLookAtNpc;
        private Transform _destTransform;

        private const float _headLookDegreeMax = 80f;

        private void LateUpdate()
        {
            if (!_doLookAtNpc)
            {
                return;
            }

            var lookRotationVector = _destTransform.position - PrefabProps.Head.position;
            var lookRotation = Quaternion.LookRotation(lookRotationVector);
            var currentHeadZ = PrefabProps.Head.rotation.eulerAngles.z;
            var targetEuler = lookRotation.eulerAngles;
            var bodyEuler = PrefabProps.Bip01.transform.rotation.eulerAngles;

            var targetXRot = targetEuler.x;
            // if (targetXRot > 180f)
            // {
            //     targetXRot -= 360f; // Convert to -180 to 180 range.
            // }
            // targetXRot = Mathf.Clamp(targetXRot, -_headLookDegreeMax, _headLookDegreeMax);

            var targetYRot = targetEuler.y;
            // targetYRot = (targetYRot + 360f) % 360f;
            // targetYRot = Mathf.Min(targetYRot, _headLookDegreeMax) + bodyEuler.y;
            // if (targetYRot > 180f)
            // {
            //     targetYRot -= 360f; // Convert to -180 to 180 range.
            // }
            // targetYRot = Mathf.Clamp(targetYRot, -_headLookDegreeMax, _headLookDegreeMax);

            var finalEuler = new Vector3(targetXRot, targetYRot, currentHeadZ);
            var finalRotation = Quaternion.Euler(finalEuler);
            var newHeadRot = Quaternion.Slerp(PrefabProps.Head.rotation, finalRotation, Time.deltaTime * 200);

            PrefabProps.Head.rotation = newHeadRot;
        }

        public void PlayAnimationRepeatedly(string animName)
        {
            // TODO
        }

        public bool PlayAnimation(string animName, string forcedNextAnimName = null)
        {
            if (!GameGlobals.Animations.PlayAnimation(PrefabProps.Animation, Properties.MdsNames, animName, out var nextAnimName))
            {
                _isAnimationPlaying = false;
                return false;
            }

            if (forcedNextAnimName.IsNullOrEmpty())
            {
                if (nextAnimName.IsNullOrEmpty())
                {
                    _nextAnimation = GetIdleAnimationName();
                }
                else
                {
                    _nextAnimation = nextAnimName;
                }
            }
            else
            {
                _nextAnimation = forcedNextAnimName;
            }

            _blendOutTime = GameGlobals.Animations.GetBlendOutTime(PrefabProps.Animation, Properties.MdsNames, animName, _nextAnimation);
            _isAnimationPlaying = true;

            return true;
        }

        public float GetAnimationLength(string animName)
        {
            return GameGlobals.Animations.GetAnimationLength(PrefabProps.Animation, Properties.MdsNames, animName);
        }

        private IEnumerator BlendOutCoroutine()
        {
            while (true)
            {
                if (!_isAnimationPlaying)
                {
                    yield return null;
                    continue;
                }

                _blendOutTime -= Time.deltaTime;
                if (_blendOutTime > 0.0f)
                {
                    // The animation is still playing and needs no "change"
                    yield return null;
                    continue;
                }

                var nextNextAnimName = GameGlobals.Animations.GetNextAnimationName(PrefabProps.Animation, Properties.MdsNames, _nextAnimation);

                // We have a loop like S_WALK and therefore the animation is played as loop. Simply stop processing now.
                if (nextNextAnimName == _nextAnimation)
                {
                    yield return null;
                    continue;
                }

                PlayAnimation(_nextAnimation, nextNextAnimName);
            }
        }

        private string GetIdleAnimationName()
        {
            string walkmode;
            switch (Properties.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    walkmode = "WALK";
                    break;
                case VmGothicEnums.WalkMode.Run:
                    walkmode = "RUN";
                    break;
                case VmGothicEnums.WalkMode.Sneak:
                    walkmode = "SNEAK";
                    break;
                case VmGothicEnums.WalkMode.Water:
                    walkmode = "WATER";
                    break;
                case VmGothicEnums.WalkMode.Swim:
                    walkmode = "SWIM";
                    break;
                case VmGothicEnums.WalkMode.Dive:
                    walkmode = "DIVE";
                    break;
                default:
                    Debug.LogWarning($"Animation of type {Properties.WalkMode} not yet implemented.");
                    return "";
            }

            return $"S_{walkmode}";
        }
    }
}
