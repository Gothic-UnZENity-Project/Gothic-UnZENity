using GUZ.Core.Data.Container;
using GUZ.Core.Models.Vm;
using GUZ.Core.Vm;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    public class PlayAniBs : PlayAni
    {
        private VmGothicEnums.BodyState _bodyState => (VmGothicEnums.BodyState)Action.Int0;


        public PlayAniBs(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            base.Start();
            Props.BodyState = _bodyState;
        }
    }
}
