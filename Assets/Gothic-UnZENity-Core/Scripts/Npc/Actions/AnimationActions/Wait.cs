using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class Wait : AbstractAnimationAction
    {
        private float _waitSeconds;

        public Wait(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            _waitSeconds = Action.Float0;
        }

        public override bool IsFinished()
        {
            _waitSeconds -= Time.deltaTime;

            return _waitSeconds <= 0f;
        }
    }
}
