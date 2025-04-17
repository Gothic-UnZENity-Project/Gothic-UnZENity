using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Daedalus;
using Vector3 = UnityEngine.Vector3;

namespace GUZ.Core._Npc2
{
    public class NpcAiManager2
    {
        public void ExtNpcPerceptionEnable(NpcInstance npc, VmGothicEnums.PerceptionType perception, int function)
        {
            npc.GetUserData2().Props.Perceptions[perception] = function;
        }

        public void ExtNpcPerceptionDisable(NpcInstance npc, VmGothicEnums.PerceptionType perception)
        {
            npc.GetUserData2().Props.Perceptions[perception] = -1;
        }

        /// <summary>
        /// Call an NPC Perception (active like Assess_Player or passive like Assess_Talk are possible).
        /// </summary>
        public void ExecutePerception(VmGothicEnums.PerceptionType type, NpcProperties2 properties, NpcInstance self, NpcInstance victim, NpcInstance other)
        {
            // Perception isn't set
            if (!properties.Perceptions.TryGetValue(type, out var perceptionFunction))
            {
                return;
            }
            // Perception is disabled
            else if (perceptionFunction < 0)
            {
                return;
            }

            var oldSelf = GameData.GothicVm.GlobalSelf;
            var oldVictim = GameData.GothicVm.GlobalVictim;
            var oldOther = GameData.GothicVm.GlobalVictim;

            GameData.GothicVm.GlobalSelf = self;
            
            if(other != null)
            {
                GameData.GothicVm.GlobalOther = other;
            }

            if(victim != null)
            {
                GameData.GothicVm.GlobalVictim = victim;
            }

            GameData.GothicVm.Call(perceptionFunction);
            
            GameData.GothicVm.GlobalSelf = oldSelf;
            GameData.GothicVm.GlobalVictim = oldVictim;
            GameData.GothicVm.GlobalVictim = oldOther;
        }

        public void ExtNpcSetPerceptionTime(NpcInstance npc, float time)
        {
            npc.GetUserData2().Props.PerceptionTime = time;
        }

        public void ExtAiSetWalkMode(NpcInstance npc, VmGothicEnums.WalkMode walkMode)
        {
            npc.GetUserData2().Props.WalkMode = walkMode;
        }

        public void ExtAiGoToWp(NpcInstance npc, string wayPointName)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new GoToWp(
                new AnimationAction(wayPointName),
                npc.GetUserData2()));
        }

        public void ExtAiAlignToWp(NpcInstance npc)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new AlignToWp(new AnimationAction(), npc.GetUserData2()));
        }

        public void ExtAiGoToFp(NpcInstance npc, string freePointName)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new GoToFp(
                new AnimationAction(freePointName),
                npc.GetUserData2()));
        }

        /// <summary>
        /// freeLOS - Free Line Of Sight == ignoreFOV
        /// </summary>
        public bool ExtNpcCanSeeNpc(NpcInstance self, NpcInstance other, bool freeLOS)
        {
            var selfContainer = self.GetUserData2();
            var otherContainer = other.GetUserData2();

            if (selfContainer == null || otherContainer == null)
            {
                return false;
            }
            // TODO: Implement a proper fix for head props being null
            // this is the case for monsters that do not have a separate head mesh 
            var selfHeadBone = selfContainer.PrefabProps.Head != null ? selfContainer.PrefabProps.Head : selfContainer.Go.transform;
            var otherHeadBone = otherContainer.PrefabProps.Head != null ? otherContainer.PrefabProps.Head : otherContainer.Go.transform;

            var distanceToNpc = Vector3.Distance(selfContainer.Go.transform.position, otherHeadBone.position);
            var inSightRange = distanceToNpc <= self.SensesRange;

            var layersToIgnore = Constants.HandLayer | Constants.GrabbableLayer;
            var hasLineOfSightCollisions = Physics.Linecast(selfHeadBone.position, otherHeadBone.position, layersToIgnore);

            var directionToTarget = (otherHeadBone.position - selfHeadBone.position).normalized;
            var angleToTarget = Vector3.Angle(selfHeadBone.forward, directionToTarget);
            var inFov = angleToTarget <= 50.0f; // OpenGothic assumes 100 fov for NPCs

            return inSightRange && !hasLineOfSightCollisions && (freeLOS || inFov);
        }

        public void ExtNpcClearAiQueue(NpcInstance npc)
        {
            npc.GetUserData2().Props.AnimationQueue.Clear();
        }

        public void ExtAiGoToNextFp(NpcInstance npc, string fpNamePart)
        {
            var npcContainer = npc.GetUserData2();
            npcContainer.Props.AnimationQueue.Enqueue(new GoToNextFp(
                new AnimationAction(fpNamePart),
                npcContainer));
        }

        public void ExtAiWait(NpcInstance npc, float seconds)
        {
            var npcContainer = npc.GetUserData2();
            npcContainer.Props.AnimationQueue.Enqueue(new Wait(
                new AnimationAction(float0: seconds),
                npcContainer));
        }

        public void ExtAiGoToNpc(NpcInstance self, NpcInstance other)
        {
            if (other == null)
            {
                return;
            }

            self.GetUserData2().Props.AnimationQueue.Enqueue(new GoToNpc(
                new AnimationAction(instance0: other),
                self.GetUserData2()));
        }

        public void ExtAiPlayAni(NpcInstance npc, string name)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new PlayAni(new AnimationAction(name), npc.GetUserData2()));
        }

        public void ExtAiStartState(NpcInstance npc, int action, bool stopCurrentState, string wayPointName)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new StartState(
                new AnimationAction(int0: action, bool0: stopCurrentState, string0: wayPointName),
                npc.GetUserData2()));
        }

        public void ExtAiLookAt(NpcInstance npc, string wayPointName)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new LookAt(new AnimationAction(wayPointName), npc.GetUserData2()));
        }

        public void ExtAiAlignToFp(NpcInstance npc)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new AlignToFp(new AnimationAction(), npc.GetUserData2()));
        }

        public void ExtAiLookAtNpc(NpcInstance npc, NpcInstance other)
        {
            if (other == null)
            {
                return;
            }

            npc.GetUserData2().Props.AnimationQueue.Enqueue(new LookAtNpc(
                new AnimationAction(instance0: other),
                npc.GetUserData2()));
        }

        public void ExtAiStopLookAt(NpcInstance npc)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new StopLookAtNpc(
                new AnimationAction(),
                npc.GetUserData2()));
        }

        public void ExtAiContinueRoutine(NpcInstance npc)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new ContinueRoutine(new AnimationAction(), npc.GetUserData2()));
        }

        public void ExtAiUseMob(NpcInstance npc, string target, int state)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new UseMob(
                new AnimationAction(target, state),
                npc.GetUserData2()));
        }

        public void ExtAiStandUp(NpcInstance npc)
        {
            // FIXME - Implement remaining tasks from G1 documentation:
            // * Ist der Nsc in einem Animatinsstate, wird die passende Rücktransition abgespielt.
            // * Benutzt der NSC gerade ein MOBSI, poppt er ins stehen.
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new StandUp(new AnimationAction(), npc.GetUserData2()));
        }

        public void ExtAiTurnToNpc(NpcInstance npc, NpcInstance other)
        {
            if (other == null)
            {
                return;
            }

            npc.GetUserData2().Props.AnimationQueue.Enqueue(new TurnToNpc(
                new AnimationAction(instance0: other),
                npc.GetUserData2()));
        }

        public void ExtAiPlayAniBs(NpcInstance npc, string name, int bodyState)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new PlayAniBs(new AnimationAction(name, bodyState), npc.GetUserData2()));
        }

        public void ExtAiUnequipArmor(NpcInstance npc)
        {
            npc.GetUserData2().Props.BodyData.Armor = 0;
        }

        /// <summary>
        /// Daedalus needs an int value.
        /// </summary>
        public int ExtNpcGetStateTime(NpcInstance npc)
        {
            // If there is no active running state, we immediately assume the current routine is running since the start of all beings.
            if (!npc.GetUserData2().Props.IsStateTimeActive)
            {
                return int.MaxValue;
            }

            return (int)npc.GetUserData2().Props.StateTime;
        }

        public void ExtNpcSetStateTime(NpcInstance npc, int seconds)
        {
            npc.GetUserData2().Props.StateTime = seconds;
        }

        /// <summary>
        /// State means the final state where the animation shall go to.
        /// example:
        /// * itemId=xyz (ItFoBeer)
        /// * animationState = 0
        /// * ItFoBeer is of visual_scheme = Potion
        /// * expected state is t_Potion_Stand_2_S0 --> s_Potion_S0
        /// </summary>
        public void ExtAiUseItemToState(NpcInstance npc, int itemId, int animationState)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new UseItemToState(
                new AnimationAction(int0: itemId, int1: animationState),
                npc.GetUserData2()));
        }

        public bool ExtNpcWasInState(NpcInstance npc, uint action)
        {
            return npc.GetUserData2().Props.PrevStateStart == action;
        }

        public VmGothicEnums.BodyState ExtGetBodyState(NpcInstance npc)
        {
            return npc.GetUserData2().Props.BodyState;
        }

        /// <summary>
        /// Return position distance in cm.
        /// </summary>
        public int ExtNpcGetDistToNpc(NpcInstance npc1, NpcInstance npc2)
        {
            if (npc1 == null || npc2 == null)
            {
                return int.MaxValue;
            }

            var npc1Pos = npc1.GetUserData2().Go.transform.position;

            Vector3 npc2Pos;
            // If hero
            if (npc2.Id == 0)
            {
                npc2Pos = Camera.main!.transform.position;
            }
            else
            {
                npc2Pos = npc2.GetUserData2().Go.transform.position;
            }

            return (int)(Vector3.Distance(npc1Pos, npc2Pos) * 100);
        }

        public void ExtAiDrawWeapon(NpcInstance npc)
        {
            npc.GetUserData2().Props.AnimationQueue.Enqueue(new DrawWeapon(new AnimationAction(), npc.GetUserData2()));
        }

        public bool ExtNpcIsDead(NpcInstance npcInstance)
        {
            // FIXME - We need to implement it properly. Just fixing NPEs for now!
            // FIXME - e.g. used for PC_Thief_AFTERTROLL_Condition() from Daedalus.
            return false;
        }

        public bool ExtNpcIsInState(NpcInstance npc, int state)
        {
            return npc.GetUserData2().Props.StateStart == state;
        }

        public bool ExtNpcIsPlayer(NpcInstance npc)
        {
            return npc.Index == GameData.GothicVm.GlobalHero!.Index;
        }

        public ItemInstance ExtGetEquippedArmor(NpcInstance npc)
        {
            var armor = npc.GetUserData2().Props.EquippedItems
                .FirstOrDefault(i => i.MainFlag == (int)VmGothicEnums.ItemFlags.ItemKatArmor);

            return armor;
        }

        public bool ExtNpcHasEquippedArmor(NpcInstance npc)
        {
            return ExtGetEquippedArmor(npc) != null;
        }

        public bool ExtNpcIsInFightMode(NpcInstance npc, VmGothicEnums.FightMode fightMode)
        {
            return npc.GetUserData2().Vob.FightMode == (int)fightMode;
        }

        public bool ExtNpcOwnedByNpc(ItemInstance item, NpcInstance npc)
        {
            if (item == null)
            {
                return false;
            }

            return item.Owner == npc.Index;
        }

        public VmGothicEnums.Attitude ExtGetAttitude(NpcInstance self, NpcInstance other)
        {
            var npc1 = self.GetUserData2();
            var npc2 = other.GetUserData2();
            if (npc1 == null || npc2 == null)
            {
                return VmGothicEnums.Attitude.Neutral;
            }

            return NpcHelper.GetPersonAttitude(npc1, npc2);
        }

        public void ExtSetAttitude(NpcInstance npc, VmGothicEnums.Attitude value)
        {
            npc.GetUserData2().Props.Attitude = value;
        }
        
        public void ExtSetTempAttitude(NpcInstance npc, VmGothicEnums.Attitude value)
        {
            npc.GetUserData2().Props.TempAttitude = value;
        }

        public bool Npc_GetTarget(NpcInstance npc)
        {
            return npc.GetUserData2().Props.TargetNpc != null;
        }

        public void Npc_SetTarget(NpcInstance npc, NpcInstance target)
        {
            npc.GetUserData2().Props.TargetNpc = target;
        }

        public void Npc_SendPassivePerc(NpcInstance npc,VmGothicEnums.PerceptionType perc, NpcInstance victim, NpcInstance other)
        {
            GameGlobals.NpcAi.ExecutePerception(perc, npc.GetUserData2().Props, npc, victim, other);
        }

        public void UpdateEnemyNpc(NpcInstance self)
        {
            var selfNpc = self.GetUserData2();
            var selfPosition = selfNpc.Go.transform.position; // Cache position

            NpcContainer2 closestEnemy = null;
            var closestSqrDist = float.MaxValue;

            foreach (var candidate in MultiTypeCache.NpcCache2)
            {
                // Fast-fail checks in order of cheapest first
                if (candidate.Props == null || candidate.Go == null)
                {
                    continue;
                }

                if (candidate.Instance.Index == self.Index)
                {
                    continue;
                }

                if (!NpcHelper.CanSenseNpc(self, candidate.Instance, true))
                {
                    continue;
                }

                if (ExtGetAttitude(self, candidate.Instance) != VmGothicEnums.Attitude.Hostile)
                {
                    continue;
                }

                // Compare squared distances to avoid sqrt calculation
                var sqrDist = (candidate.Go.transform.position - selfPosition).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closestEnemy = candidate;
                }
            }

            selfNpc.Props.EnemyNpc = closestEnemy?.Instance;
        }
    }
}
