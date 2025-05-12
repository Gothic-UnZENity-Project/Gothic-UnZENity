using GUZ.Core.Data.Container;
using GUZ.Core.Globals;
using ZenKit.Daedalus;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class StartState : AbstractAnimationAction
    {
        private int _action => Action.Int0;
        private string _wayPoint => Action.String0;
        private NpcInstance _other => Action.Instance0;
        private NpcInstance _victim => Action.Instance1;
        
        public StartState(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            var ai = PrefabProps.AiHandler;

            Vob.NextStateIndex = _action;
            Vob.NextStateName = GameData.GothicVm.GetSymbolByIndex(_action)!.Name;
            Vob.NextStateValid = true;
            
            ai.ClearState(Action.Bool0);

            Props.IsStateTimeActive = true;
            Props.StateTime = 0;

            GameData.GothicVm.GlobalOther = _other;
            GameData.GothicVm.GlobalVictim = _victim;
        }

        /// <summary>
        /// This one is actually no animation, but we need to call Start() only.
        /// </summary>
        public override bool IsFinished()
        {
            return true;
        }
    }
}
