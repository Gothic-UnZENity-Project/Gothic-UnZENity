using System;
using GUZ.Core.Data.Container;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using ZenKit.Daedalus;
using Random = UnityEngine.Random;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class Attack : AbstractAnimationAction
    {
        private FightAiMove _move;
        
        
        public Attack(AnimationAction action, NpcContainer npcData) : base(action, npcData)
        {
        }

        public override void Start()
        {
            var aiFunctionTemplate = FindAiFunctionTemplate();
            _move = FindAttackAction(aiFunctionTemplate);
            StartAttackAction();
        }

        private string FindAiFunctionTemplate()
        {
            switch ((VmGothicEnums.WeaponState)Vob.FightMode)
            {
                case VmGothicEnums.WeaponState.Fist:
                case VmGothicEnums.WeaponState.W1H:
                case VmGothicEnums.WeaponState.W2H:
                    return FightConst.AttackActions.MyWFocus;
                    break;
                case VmGothicEnums.WeaponState.NoWeapon:
                case VmGothicEnums.WeaponState.Bow:
                case VmGothicEnums.WeaponState.CBow:
                case VmGothicEnums.WeaponState.Mage:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private FightAiMove FindAttackAction(string nameTemplate)
        {
            var fightAi = VmInstanceManager.TryGetFightAiData(nameTemplate, Vob.FightTactic);
            int moveCount;
            for (moveCount = 0; moveCount < FightConst.FightAiMoveMax; moveCount++)
            {
                // Load all move entries in list until the value is 0 aka unset.
                if (fightAi.GetMove(moveCount) == FightAiMove.Nop)
                    break;
            }
            
            return fightAi.GetMove(Random.Range(0, moveCount-1));
        }

        private void StartAttackAction()
        {
            switch (_move)
            {
                case FightAiMove.Wait:
                    // We reuse this flag and close the attack action after 200ms.
                    AnimationEndEventTime = 0.2f;
                    // FIXME - Call idle animation while waiting.
                    break;
                case FightAiMove.Run:
                case FightAiMove.RunBack:
                case FightAiMove.JumpBack:
                case FightAiMove.Turn:
                case FightAiMove.Strafe:
                case FightAiMove.Attack:
                case FightAiMove.AttackSide:
                case FightAiMove.AttackFront:
                case FightAiMove.AttackTriple:
                case FightAiMove.AttackWhirl:
                case FightAiMove.AttackMaster:
                case FightAiMove.TurnToHit:
                case FightAiMove.Parry:
                case FightAiMove.StandUp:
                case FightAiMove.WaitLonger:
                case FightAiMove.WaitExt:
                    Logger.LogError($"Ai_Attack() type >{_move}< not yet handled. Skipping...", LogCat.Ai);
                    IsFinishedFlag = true;
                    break;
                case FightAiMove.Nop:
                default:
                    if (_move == FightAiMove.Nop)
                    {
                        Logger.LogError("No action for Ai_Attack() selected. Missing path in logic!", LogCat.Ai);
                        IsFinishedFlag = true;
                    }
                    break;
            }
        }

        public override void Tick()
        {
            base.Tick();
        }
    }
}
