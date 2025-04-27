using GUZ.Core._Npc2;
using GUZ.Core.Creator;
using GUZ.Core.Vm;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class PlayAniBs : PlayAni
    {
        private VmGothicEnums.BodyState _bodyState => (VmGothicEnums.BodyState)Action.Int0;


        public PlayAniBs(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            base.Start();
            Props.BodyState = _bodyState;
        }
    }
}
