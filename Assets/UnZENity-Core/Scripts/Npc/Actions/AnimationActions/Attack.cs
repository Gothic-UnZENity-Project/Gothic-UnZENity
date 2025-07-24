using System;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using ZenKit.Daedalus;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class Attack : AbstractAnimationAction
    {
        private NpcInstance _enemy => (NpcInstance)GameData.GothicVm.GlobalVictim;
        
        private FightAiMove _move;
        
        
        public Attack(AnimationAction action, NpcContainer npcData) : base(action, npcData)
        {
        }

        public override void Start()
        {
            var aiFunctionTemplate = FindAiFunctionTemplate();
            _move = VmInstanceManager.TryGetFightAiData(aiFunctionTemplate, Vob.FightTactic).GetRandomMove();
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
                case VmGothicEnums.WeaponState.NoWeapon:
                case VmGothicEnums.WeaponState.Bow:
                case VmGothicEnums.WeaponState.CBow:
                case VmGothicEnums.WeaponState.Mage:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StartAttackAction()
        {
            switch (_move)
            {
                case FightAiMove.Wait:
                    // We reuse this flag and close the attack action after 200ms.
                    GameGlobals.NpcAi.ExtAiWait(NpcInstance, 0.2f);
                    break;
                case FightAiMove.Attack:
                    if (IsInFightRange())
                    {
                        // TODO - In the future, we need to handle more information than just playing the attack animation. But fine for the first iteration.
                        GameGlobals.NpcAi.ExtAiPlayAni(NpcInstance, GetAnimName(VmGothicEnums.AnimationType.Attack));
                    }
                    else
                    {
                        // FIXME - DEBUG. Need to handle differently.
                    }
                    break;
                case FightAiMove.Run:
                case FightAiMove.RunBack:
                case FightAiMove.JumpBack:
                case FightAiMove.Turn:
                case FightAiMove.Strafe:
                    if (Random.Range(0, 2) == 0)
                        GameGlobals.NpcAi.ExtAiPlayAni(NpcInstance, GetAnimName(VmGothicEnums.AnimationType.MoveL));
                    else
                        GameGlobals.NpcAi.ExtAiPlayAni(NpcInstance, GetAnimName(VmGothicEnums.AnimationType.MoveR));
                    break;
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
                    Logger.LogError("No action for Ai_Attack() selected. Missing path in logic!", LogCat.Ai);
                    IsFinishedFlag = true;
                    break;
            }
            
            IsFinishedFlag = true;
        }

        /// <summary>
        /// Fight range is calculated by base range + weapon attack range.
        /// </summary>
        private bool IsInFightRange()
        {
            var dist = Vector3.Distance(NpcGo.transform.position, _enemy.GetUserData()!.Go.transform.position);
            var attackRange = GameData.GuildValues.GetFightRangeBase(Vob.GuildTrue) +
                              GameData.GuildValues.GetFightRangeFist(Vob.GuildTrue);
            
            // FIXME - Currently we assume Fist only. We need to set range for weapons properly as well. (e.g., Orcs)
            
            return dist < (attackRange / 100f); // m -> cm
        }

        /// <summary>
        /// Short cut method
        /// </summary>
        private string GetAnimName(VmGothicEnums.AnimationType type)
        {
            return GameGlobals.Animations.GetAnimationName(type, Vob);
        }

        public override void Tick()
        {
            base.Tick();
        }
    }
}
