using System.Collections;
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

        public void PlayAnimation(string animName, string nextAnimName)
        {
            if (!GameGlobals.Animations.PlayAnimation(PrefabProps.NpcAnimation, Properties.MdsNames, animName))
            {
                return;
            }

            _blendOutTime = GameGlobals.Animations.GetBlendOutTime(PrefabProps.NpcAnimation, Properties.MdsNames, animName, nextAnimName);
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
    }
}
