using System;
using GUZ.Core.Models.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Const;
using GUZ.Core.Logging;
using GUZ.Core.Models.Vm;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Npc;
using GUZ.Core.Util;
using Reflex.Attributes;
using ZenKit.Daedalus;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    public class Attack : AbstractAnimationAction
    {
        [Inject] private readonly AnimationService _animationService;
        [Inject] private readonly NpcAiService _npcAiService;
        [Inject] private readonly GameStateService _gameStateService;

        private NpcInstance _enemy => (NpcInstance)_gameStateService.GothicVm.GlobalVictim;
        
        private FightAiMove _move;
        
        
        public Attack(AnimationAction action, NpcContainer npcData) : base(action, npcData)
        {
        }

        public override void Start()
        {
            var aiFunctionTemplate = FindAiFunctionTemplate();
            _move = VmCacheService.TryGetFightAiData(aiFunctionTemplate, Vob.FightTactic).GetRandomMove();
            StartAttackAction();
        }

        private string FindAiFunctionTemplate()
        {
            var isInFocus = _npcAiService.ExtNpcCanSeeNpc(NpcInstance, _enemy, false, 30f); // 60° is also assumed by Open Gothic.
            var distance = GetDistance();
            var attackRange = GetAttackRange();
            var isInWRange = distance <= attackRange; // W-Range == Weapon range
            var isInGRange = !isInWRange && distance <= attackRange * 3; // G-Range == Goto range
            var isInFKRange = !isInWRange && !isInGRange; // FK-Range == Fernkampf range; Yes, in G1 it's assumed that nothing is farther away than 30 m, but I don't expect any AI_Attack() call to be at this range.
            var isRunning = false; // FIXME - We need to handle this for >MyGRunTo<

            switch ((VmGothicEnums.WeaponState)Vob.FightMode)
            {
                case VmGothicEnums.WeaponState.Fist:
                case VmGothicEnums.WeaponState.W1H:
                case VmGothicEnums.WeaponState.W2H:
                    if (isInWRange)
                        return isInFocus ? FightConst.AttackActions.MyWFocus : FightConst.AttackActions.MyWNoFocus;
                    if (isInGRange)
                        return isInFocus ? FightConst.AttackActions.MyGFocus : FightConst.AttackActions.MyGFkNoFocus;
                    else
                        return isInFocus ? FightConst.AttackActions.MyFkFocus : FightConst.AttackActions.MyGFkNoFocus;
                case VmGothicEnums.WeaponState.NoWeapon:
                case VmGothicEnums.WeaponState.Bow:
                case VmGothicEnums.WeaponState.CBow:
                case VmGothicEnums.WeaponState.Mage:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // FIXME - In the future, we need to handle more information than just playing the attack animations. But fine for the first iteration.
        private void StartAttackAction()
        {
            switch (_move)
            {
                case FightAiMove.Wait:
                    // We reuse this flag and close the attack action after 200ms.
                    _npcAiService.ExtAiWait(NpcInstance, 0.2f);
                    break;
                case FightAiMove.Attack:
                    _npcAiService.ExtAiPlayAni(NpcInstance, GetAnimName(VmGothicEnums.AnimationType.Attack));
                    break;
                case FightAiMove.Strafe:
                    if (Random.Range(0, 2) == 0)
                        _npcAiService.ExtAiPlayAni(NpcInstance, GetAnimName(VmGothicEnums.AnimationType.MoveL));
                    else
                        _npcAiService.ExtAiPlayAni(NpcInstance, GetAnimName(VmGothicEnums.AnimationType.MoveR));
                    break;
                case FightAiMove.Run:
                    _npcAiService.ExtAiPlayAni(NpcInstance, GetAnimName(VmGothicEnums.AnimationType.Move));
                    break;
                case FightAiMove.Turn:
                    _npcAiService.ExtAiTurnToNpc(NpcInstance, _enemy);
                    break;
                case FightAiMove.RunBack:
                case FightAiMove.JumpBack:
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

        private float GetDistance()
        {
            return Vector3.Distance(NpcGo.transform.position, _enemy.GetUserData()!.Go.transform.position);
        }

        /// Fight range is calculated by base range + weapon attack range.
        private float GetAttackRange()
        {
            var baseRange = _gameStateService.GuildValues.GetFightRangeBase(Vob.GuildTrue);
            // FIXME - Currently we assume Fist only. We need to set range for weapons properly as well. (e.g., Orcs)
            var weaponRange = _gameStateService.GuildValues.GetFightRangeFist(Vob.GuildTrue);
            return (baseRange + weaponRange) / 100f; // m -> cm
        }

        /// <summary>
        /// Short cut method
        /// </summary>
        private string GetAnimName(VmGothicEnums.AnimationType type)
        {
            return _animationService.GetAnimationName(type, Vob);
        }

        public override void Tick()
        {
            base.Tick();
        }
    }
}
