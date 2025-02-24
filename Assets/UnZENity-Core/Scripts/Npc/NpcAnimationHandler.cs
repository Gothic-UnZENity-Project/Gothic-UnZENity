using System.Collections;
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
            NpcData.PrefabProps.NpcAnimation = GetComponent<Animation>();
        }

        private void Start()
        {
            StartCoroutine(BlendOutCoroutine());
        }

        public void PlayAnimationRepeatedly(string animName)
        {
            // TODO
        }

        public void PlayAnimation(string animName, [CanBeNull] string nextAnimName)
        {
            if (!GameGlobals.Animations.PlayAnimation(PrefabProps.NpcAnimation, Properties.MdsNames, animName))
            {
                return;
            }

            if (nextAnimName.IsNullOrEmpty())
            {
                _nextAnimation = GetIdleAnimationName();
            }
            else
            {
                _nextAnimation = nextAnimName;
            }

            _blendOutTime = GameGlobals.Animations.GetBlendOutTime(PrefabProps.NpcAnimation, Properties.MdsNames, animName, _nextAnimation);
            _isAnimationPlaying = true;
        }

        private IEnumerator BlendOutCoroutine()
        {
            while (true)
            {
                if (!_isAnimationPlaying)
                {
                    yield return null;
                }
                else
                {
                    yield return new WaitForSeconds(_blendOutTime);
                    _isAnimationPlaying = false;

                    var nextNextAnimName = GameGlobals.Animations.GetNextAnimationName(PrefabProps.NpcAnimation, Properties.MdsNames, _nextAnimation);

                    // We have a loop like S_WALK and therefore the animation is played as loop. Simply stop processing now.
                    if (nextNextAnimName == _nextAnimation)
                    {
                        yield return null;
                    }

                    PlayAnimation(_nextAnimation, nextNextAnimName);
                }
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
