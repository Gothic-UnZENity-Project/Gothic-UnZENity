using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Data;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Npc;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit.Daedalus;
using Object = UnityEngine.Object;

namespace GUZ.Core.Manager
{
    public static class NpcHelper
    {
        /// <summary>
        /// Ranges are in meter.
        ///
        /// FIXME - We should use PERC_ASSESSTALK range to leverage HVR's Grabbable hover and remote grab distance!
        /// </summary>
        public static readonly Dictionary<VmGothicEnums.PerceptionType, int> PerceptionRanges = new ();


        private const float _fpLookupDistance = 7f; // meter


        static NpcHelper()
        {
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(CacheHero);
        }

        public static void Init()
        {
            // Perceptions
            var percInitSymbol = GameData.GothicVm.GetSymbolByName("InitPerceptions");
            if (percInitSymbol == null)
            {
                Debug.LogError("InitPerceptions symbol not found.");
            }
            else
            {
                GameData.GothicVm.Call(percInitSymbol.Index);
            }
        }

        /// <summary>
        /// We need to first Alloc() hero data space and put the instance to the cache.
        /// Then we initialize it. (During Init, PC_HERO:Npc_Default->Prototype:Npc_Default will call SetTalentValue where we need the lookup to fetch the NpcInstance).
        ///
        /// This method will get called every time we spawn into another world. We therefore need to check if initialize the first time or we only need to set the lookup cache.
        /// </summary>
        public static void CacheHero()
        {
            if (GameData.GothicVm.GlobalHero != null)
            {
                return;
            }


            // Initial setup
            var playerGo = GameObject.FindWithTag(Constants.PlayerTag);

            // Flat player
            if (playerGo == null)
            {
                playerGo = GameObject.FindWithTag(Constants.MainCameraTag);
            }


            var heroInstance = GameData.GothicVm.AllocInstance<NpcInstance>(GameGlobals.Settings.IniPlayerInstanceName);
            var playerProperties = playerGo.GetComponent<NpcProperties>();
            var vobNpc = new ZenKit.Vobs.Npc();

            playerProperties.SetData(vobNpc);
            playerProperties.NpcInstance = heroInstance;
            playerProperties.Head = Camera.main!.transform;

            var npcData = new NpcData
            {
                Instance = heroInstance,
                Vob = vobNpc,
                Properties = playerProperties
            };

            heroInstance.UserData = npcData;

            MultiTypeCache.NpcCache.Add(npcData);
            
            GameData.GothicVm.InitInstance(heroInstance);
            GameData.GothicVm.GlobalHero = heroInstance;
        }

        /// <summary>
        /// Call an NPC Perception (active like Assess_Player or passive like Assess_Talk are possible).
        /// </summary>
        public static void ExecutePerception(VmGothicEnums.PerceptionType type, NpcProperties properties, NpcInstance self, NpcInstance other)
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

            GameData.GothicVm.GlobalSelf = self;
            GameData.GothicVm.GlobalOther = other;
            GameData.GothicVm.Call(perceptionFunction);
        }

        public static void ExtPErcSetRange(int perceptionId, int rangeInCm)
        {
            PerceptionRanges[(VmGothicEnums.PerceptionType)perceptionId] = rangeInCm / 100;
        }

        public static bool ExtIsMobAvailable(NpcInstance npcInstance, string vobName)
        {
            var npc = GetNpc(npcInstance);
            var vob = VobHelper.GetFreeInteractableWithin10M(npc.transform.position, vobName);

            return vob != null;
        }

        public static bool ExtWldIsFpAvailable(NpcInstance npc, string fpNamePart)
        {
            var props = GetProperties(npc);
            var npcGo = props.Go;
            var freePoints =
                WayNetHelper.FindFreePointsWithName(npcGo.transform.position, fpNamePart, _fpLookupDistance);

            foreach (var fp in freePoints)
            {
                // Kind of: If we're already standing on a FreePoint, then there is one available.
                if (props.CurrentFreePoint == fp)
                {
                    return true;
                }

                // Alternatively, we found a free one within range.
                if (!fp.IsLocked)
                {
                    return true;
                }
            }

            return false;
        }

        public static string ExtGetNearestWayPoint(NpcInstance npc)
        {
            var pos = GetProperties(npc).transform.position;

            return WayNetHelper.FindNearestWayPoint(pos).Name;
        }

        public static bool ExtIsNextFpAvailable(NpcInstance npc, string fpNamePart)
        {
            var props = GetProperties(npc);
            var pos = props.transform.position;
            var fp = WayNetHelper.FindNearestFreePoint(pos, fpNamePart);

            if (fp == null)
            {
                return false;
            }
            // Ignore if we're already on this FP.

            if (fp == props.CurrentFreePoint)
            {
                return false;
            }

            if (fp.IsLocked)
            {
                return false;
            }

            return true;
        }

        public static int ExtWldGetMobState(NpcInstance npcInstance, string scheme)
        {
            var npcGo = GetNpc(npcInstance);

            var props = GetProperties(npcInstance);

            VobProperties vob;

            if (props.CurrentInteractable != null)
            {
                vob = props.CurrentInteractable.GetComponent<VobProperties>();
            }
            else
            {
                vob = VobHelper.GetFreeInteractableWithin10M(npcGo.transform.position, scheme);
            }

            if (vob == null || vob.VisualScheme != scheme)
            {
                return -1;
            }

            if (vob is VobInteractiveProperties interactiveVob)
            {
                return Math.Max(0, interactiveVob.InteractiveProperties.State);
            }

            return -1;
        }

        public static ItemInstance ExtGetEquippedArmor(NpcInstance npc)
        {
            var armor = GetProperties(npc).EquippedItems
                .FirstOrDefault(i => i.MainFlag == (int)VmGothicEnums.ItemFlags.ItemKatArmor);

            return armor;
        }

        public static bool ExtNpcHasEquippedArmor(NpcInstance npc)
        {
            return ExtGetEquippedArmor(npc) != null;
        }

        public static ItemInstance ExtNpcGetEquippedMeleeWeapon(NpcInstance npc)
        {
            var meleeWeapon = GetProperties(npc).EquippedItems
                .FirstOrDefault(i => i.MainFlag == (int)VmGothicEnums.ItemFlags.ItemKatNf);

            return meleeWeapon;
        }

        public static bool ExtNpcHasEquippedMeleeWeapon(NpcInstance npc)
        {
            return ExtNpcGetEquippedMeleeWeapon(npc) != null;
        }

        public static ItemInstance ExtNpcGetEquippedRangedWeapon(NpcInstance npc)
        {
            var rangedWeapon = GetProperties(npc).EquippedItems
                .FirstOrDefault(i => i.MainFlag == (int)VmGothicEnums.ItemFlags.ItemKatFf);

            return rangedWeapon;
        }

        public static bool ExtNpcHasEquippedRangedWeapon(NpcInstance npc)
        {
            return ExtNpcGetEquippedRangedWeapon(npc) != null;
        }

        public static bool ExtIsNpcOnFp(NpcInstance npc, string vobNamePart)
        {
            var freePoint = GetProperties(npc).CurrentFreePoint;

            if (freePoint == null)
            {
                return false;
            }

            return freePoint.Name.ContainsIgnoreCase(vobNamePart);
        }

        /// <summary>
        /// As WldDetectNpc and WldDetectNpc seem to be the same logic except one parameter,
        /// we implement both in this function.
        /// </summary>
        public static bool ExtWldDetectNpcEx(NpcInstance npcInstance, int specificNpcIndex, int aiState, int guild,
            bool detectPlayer)
        {
            var npc = GetProperties(npcInstance);
            var npcPos = npc.transform.position;

            // FIXME - Add Guild check
            // FIXME - add range check based on perceiveAll's range (npc.sense_range)
            var foundNpc = MultiTypeCache.NpcCache
                .Where(i => i.Properties != null) // ignore empty (safe check)
                .Where(i => i.Properties.Go != null) // ignore empty (safe check)
                .Where(i => i.Instance.Index != npcInstance.Index) // ignore self
                .Where(i => detectPlayer ||
                            i.Instance.Index !=
                            GameData.GothicVm.GlobalHero!.Index) // if we don't detect player, then skip it
                .Where(i => specificNpcIndex < 0 ||
                            specificNpcIndex == i.Instance.Index) // Specific NPC is found right now?
                .Where(i => aiState < 0 || npc.State == i.Properties.State)
                .OrderBy(i => Vector3.Distance(i.Properties.transform.position, npcPos)) // get nearest
                .FirstOrDefault();

            // without this Dialog box stops and breaks the entire NPC logic
            if(foundNpc == null){
                return false;
            }

            // We need to set it, as there are calls where we immediately need _other_. e.g.:
            // if (Wld_DetectNpc(self, ...) && (Npc_GetDistToNpc(self, other)<HAI_DIST_SMALLTALK)
            if (foundNpc.Instance != null)
            {
                GameData.GothicVm.GlobalOther = foundNpc.Instance;
            }

            return foundNpc.Instance != null;
        }

        public static int ExtNpcHasItems(NpcInstance npc, uint itemId)
        {
            if (GetProperties(npc).Items.TryGetValue(itemId, out var amount))
            {
                return amount;
            }

            return 0;
        }

        public static int ExtNpcGetDistToWp(NpcInstance npc, string waypointName)
        {
            var npcGo = GetNpc(npc);
            var npcPos = npcGo.transform.position;

            var waypoint = WayNetHelper.GetWayNetPoint(waypointName);

            if (waypoint == null || !npcGo)
            {
                return int.MaxValue;
            }

            return (int)Vector3.Distance(npcPos, waypoint.Position);
        }

        /// <summary>
        /// freeLOS - Free Line Of Sight == ignoreFOV
        /// </summary>
        public static bool ExtNpcCanSeeNpc(NpcInstance self, NpcInstance other, bool freeLOS)
        {
            var selfProps = GetProperties(self);
            var otherProps = GetProperties(other);

            if (selfProps == null || otherProps == null)
            {
                return false;
            }

            var selfHeadBone = selfProps.Head;
            var otherHeadBone = otherProps.Head;

            var distanceToNpc = Vector3.Distance(selfProps.transform.position, otherHeadBone.position);
            var inSightRange =  distanceToNpc <= self.SensesRange;

            var layersToIgnore = Constants.HandLayer | Constants.GrabbableLayer;
            var hasLineOfSightCollisions = Physics.Linecast(selfHeadBone.position, otherHeadBone.position, layersToIgnore);

            var directionToTarget = (otherHeadBone.position - selfHeadBone.position).normalized;
            var angleToTarget = Vector3.Angle(selfHeadBone.forward, directionToTarget);
            var inFov = angleToTarget <= 50.0f; // OpenGothic assumes 100 fov for NPCs

            return inSightRange && !hasLineOfSightCollisions && (freeLOS || inFov);
        }

        public static void ExtNpcClearAiQueue(NpcInstance npc)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Clear();
        }

        public static void ExtNpcClearInventory(NpcInstance npc)
        {
            var props = GetProperties(npc);
            props.Items.Clear();
        }

        public static string ExtNpcGetNextWp(NpcInstance npc)
        {
            var pos = GetProperties(npc).transform.position;

            return WayNetHelper.FindNearestWayPoint(pos, true).Name;
        }

        public static int ExtNpcGetTalentSkill(NpcInstance npc, int skillId)
        {
            var props = GetProperties(npc);

            // FIXME - this is related to overlays for the npc's
            return 0;
        }

        public static int ExtNpcGetTalentValue(NpcInstance npc, int skillId)
        {
            return GetProperties(npc).Talents[(VmGothicEnums.Talent)skillId];
        }

        [CanBeNull]
        private static GameObject GetNpc([CanBeNull] NpcInstance npc)
        {
            return GetProperties(npc)?.Go;
        }

        private static NpcProperties GetProperties([CanBeNull] NpcInstance npc)
        {
            return npc?.GetUserData().Properties;
        }

        public static void ExtAiWait(NpcInstance npc, float seconds)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new Wait(
                new AnimationAction(float0: seconds),
                props.Go));
        }

        public static void ExtAiUseMob(NpcInstance npc, string target, int state)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new UseMob(
                new AnimationAction(target, state),
                props.Go));
        }

        public static void ExtAiStandUp(NpcInstance npc)
        {
            // FIXME - Implement remaining tasks from G1 documentation:
            // * Ist der Nsc in einem Animatinsstate, wird die passende Rücktransition abgespielt.
            // * Benutzt der NSC gerade ein MOBSI, poppt er ins stehen.
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new StandUp(new AnimationAction(), props.Go));
        }

        public static void ExtAiSetWalkMode(NpcInstance npc, VmGothicEnums.WalkMode walkMode)
        {
            GetProperties(npc).WalkMode = walkMode;
        }

        public static void ExtAiGoToFp(NpcInstance npc, string freePointName)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new GoToFp(
                new AnimationAction(freePointName),
                props.Go));
        }

        public static void ExtAiGoToNextFp(NpcInstance npc, string fpNamePart)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new GoToNextFp(
                new AnimationAction(fpNamePart),
                props.Go));
        }

        public static void ExtAiGoToWp(NpcInstance npc, string wayPointName)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new GoToWp(
                new AnimationAction(wayPointName),
                props.Go));
        }

        public static void ExtAiGoToNpc(NpcInstance self, NpcInstance other)
        {
            if (other == null)
            {
                return;
            }

            var props = GetProperties(self);
            props.AnimationQueue.Enqueue(new GoToNpc(
                new AnimationAction(instance0: other),
                props.Go));
        }

        public static void ExtAiAlignToFp(NpcInstance npc)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new AlignToFp(new AnimationAction(), props.Go));
        }

        public static void ExtAiAlignToWp(NpcInstance npc)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new AlignToWp(new AnimationAction(), props.Go));
        }

        public static void ExtAiPlayAni(NpcInstance npc, string name)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new PlayAni(new AnimationAction(name), props.Go));
        }

        public static void ExtAiStartState(NpcInstance npc, int action, bool stopCurrentState, string wayPointName)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new StartState(
                new AnimationAction(int0: action, bool0: stopCurrentState, string0: wayPointName),
                props.Go));
        }

        public static void ExtAiLookAt(NpcInstance npc, string wayPointName)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new LookAt(new AnimationAction(wayPointName), props.Go));
        }

        public static void ExtAiLookAtNpc(NpcInstance npc, NpcInstance other)
        {
            if (other == null)
            {
                return;
            }

            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new LookAtNpc(
                new AnimationAction(instance0: other),
                props.Go));
        }

        public static void ExtAiContinueRoutine(NpcInstance npc)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new ContinueRoutine(new AnimationAction(), props.Go));
        }

        public static void ExtAiTurnToNpc(NpcInstance npc, NpcInstance other)
        {
            if (other == null)
            {
                return;
            }

            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new TurnToNpc(
                new AnimationAction(instance0: other),
                props.Go));
        }

        public static void ExtAiPlayAniBs(NpcInstance npc, string name, int bodyState)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new PlayAniBs(new AnimationAction(name, bodyState), props.Go));
        }

        public static void ExtAiUnequipArmor(NpcInstance npc)
        {
            var props = GetProperties(npc);
            props.BodyData.Armor = 0;
        }

        /// <summary>
        /// Daedalus needs an int value.
        /// </summary>
        public static int ExtNpcGetStateTime(NpcInstance npc)
        {
            var props = GetProperties(npc);

            // If there is no active running state, we immediately assume the current routine is running since the start of all beings.
            if (!props.IsStateTimeActive)
            {
                return int.MaxValue;
            }

            return (int)props.StateTime;
        }

        public static void ExtNpcSetStateTime(NpcInstance npc, int seconds)
        {
            GetProperties(npc).StateTime = seconds;
        }

        /// <summary>
        /// State means the final state where the animation shall go to.
        /// example:
        /// * itemId=xyz (ItFoBeer)
        /// * animationState = 0
        /// * ItFoBeer is of visual_scheme = Potion
        /// * expected state is t_Potion_Stand_2_S0 --> s_Potion_S0
        /// </summary>
        public static void ExtAiUseItemToState(NpcInstance npc, int itemId, int animationState)
        {
            var props = GetProperties(npc);
            props.AnimationQueue.Enqueue(new UseItemToState(
                new AnimationAction(int0: itemId, int1: animationState),
                props.Go));
        }

        public static bool ExtNpcWasInState(NpcInstance npc, uint action)
        {
            var props = GetProperties(npc);
            return props.PrevStateStart == action;
        }

        public static VmGothicEnums.BodyState ExtGetBodyState(NpcInstance npc)
        {
            return GetProperties(npc).BodyState;
        }

        /// <summary>
        /// Return position distance in cm.
        /// </summary>
        public static int ExtNpcGetDistToNpc(NpcInstance npc1, NpcInstance npc2)
        {
            if (npc1 == null || npc2 == null)
            {
                return int.MaxValue;
            }

            var npc1Pos = npc1.GetUserData().Go.transform.position;

            Vector3 npc2Pos;
            // If hero
            if (npc2.Id == 0)
            {
                npc2Pos = Camera.main!.transform.position;
            }
            else
            {
                npc2Pos = npc2.GetUserData().Go.transform.position;
            }

            return (int)(Vector3.Distance(npc1Pos, npc2Pos) * 100);
        }

        public static void ExtAiDrawWeapon(NpcInstance npc)
        {
            var props = GetProperties(npc);

            props.AnimationQueue.Enqueue(new DrawWeapon(new AnimationAction(), props.Go));
        }

        public static void ExtNpcExchangeRoutine(NpcInstance npcInstance, string routineName)
        {
            var formattedRoutineName = $"Rtn_{routineName}_{npcInstance.Id}";
            var newRoutine = GameData.GothicVm.GetSymbolByName(formattedRoutineName);

            if (newRoutine == null)
            {
                Debug.LogError($"Routine {formattedRoutineName} couldn't be found.");
                return;
            }

            ExchangeRoutine(npcInstance, newRoutine.Index);
        }

        public static bool ExtNpcIsDead(NpcInstance npcInstance)
        {
            // FIXME - We need to implement it properly. Just fixing NPEs for now!
            // FIXME - e.g. used for PC_Thief_AFTERTROLL_Condition() from Daedalus.
            return false;
        }
        
        public static bool ExtNpcIsInState(NpcInstance npc, int state)
        {
            // FIXME - We need to implement it properly. Just fixing NPEs for now!
            // FIXME - e.g. used for PC_Thief_AFTERTROLL_Condition() from Daedalus.
            return false;
        }

        public static void ExtNpcSetToFistMode(NpcInstance npcInstance)
        {
            var npcProperties = GetProperties(npcInstance);

            npcProperties.WeaponState = VmGothicEnums.WeaponState.Fist;
            // npc.properties.
            // if npc has item in hand remove it and set weapon to fist 
            // Some animations need to force remove items, some not.
            if (npcProperties.UsedItemSlot == "")
            {
                return;
            }

            var slotGo = npcProperties.Go.FindChildRecursively(npcProperties.UsedItemSlot);
            var item = slotGo!.transform.GetChild(0);

            Object.Destroy(item.gameObject);
        }

        public static bool ExtNpcIsPlayer(NpcInstance npc)
        {
            return npc.Index == GameData.GothicVm.GlobalHero!.Index;
        }

        /// <summary>
        /// We only fully reload routines for an NPC, but do not execute any of them.
        /// This is done at a later stage when ZS_*_END of "old" routine is finalized.
        /// </summary>
        public static void ExchangeRoutine(NpcInstance npcInstance, int routineIndex)
        {
            var go = npcInstance.GetUserData().Go;
            // e.g. Monsters have no routine and we just need to send ai
            if (routineIndex == 0)
            {
                // We always need to set "self" before executing any Daedalus function.
                GameData.GothicVm.GlobalSelf = npcInstance;
                go.GetComponent<AiHandler>().StartRoutine(npcInstance.StartAiState);
                return;
            }

            var routineComp = go.GetComponent<Routine>();
            routineComp.Routines.Clear();

            // We always need to set "self" before executing any Daedalus function.
            GameData.GothicVm.GlobalSelf = npcInstance;
            GameData.GothicVm.Call(routineIndex);

            routineComp.CalculateCurrentRoutine();
        }

        public static GameObject GetHeroGameObject()
        {
            return ((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData().Go;
        }
    }
}
