﻿using System;
using System.Globalization;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Creator;
using GUZ.Core.Data.Container;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using ZenKit;
using ZenKit.Daedalus;
using Logger = GUZ.Core.Util.Logger;
using Random = UnityEngine.Random;
using Vector3 = System.Numerics.Vector3;

namespace GUZ.Core.Vm
{
    /// <summary>
    /// Contains basic methods only available in Gothic Daedalus module.
    /// </summary>
    public static class VmGothicExternals
    {
        private static bool _enableZSpyLogs;
        private static int _zSpyChannel;

        public static void RegisterExternals()
        {
            _enableZSpyLogs = GameGlobals.Config.Dev.EnableZSpyLogs;
            _zSpyChannel = GameGlobals.Config.Dev.ZSpyChannel;

            var vm = GameData.GothicVm;
            vm.RegisterExternalDefault(DefaultExternal);

            // AI
            vm.RegisterExternal<NpcInstance>("AI_StandUp", AI_StandUp);
            vm.RegisterExternal<NpcInstance>("AI_StandUpQuick", AI_StandUpQuick);
            vm.RegisterExternal<NpcInstance, int>("AI_SetWalkMode", AI_SetWalkMode);
            vm.RegisterExternal<NpcInstance>("AI_AlignToFP", AI_AlignToFP);
            vm.RegisterExternal<NpcInstance>("AI_AlignToWP", AI_AlignToWP);
            vm.RegisterExternal<NpcInstance, string>("AI_GotoFP", AI_GotoFP);
            vm.RegisterExternal<NpcInstance, string>("AI_GotoWP", AI_GotoWP);
            vm.RegisterExternal<NpcInstance, NpcInstance>("AI_GotoNpc", AI_GotoNpc);
            vm.RegisterExternal<NpcInstance, string>("AI_PlayAni", AI_PlayAni);
            vm.RegisterExternal<NpcInstance, int, int, string>("AI_StartState", AI_StartState);
            vm.RegisterExternal<NpcInstance, int, int>("AI_UseItemToState", AI_UseItemToState);
            vm.RegisterExternal<NpcInstance, float>("AI_Wait", AI_Wait);
            vm.RegisterExternal<NpcInstance, int>("AI_WaitMs", AI_WaitMs);
            vm.RegisterExternal<int, NpcInstance, string, int>("AI_UseMob", AI_UseMob);
            vm.RegisterExternal<NpcInstance, string>("AI_GoToNextFP", AI_GoToNextFP);
            vm.RegisterExternal<NpcInstance>("AI_DrawWeapon", AI_DrawWeapon);
            vm.RegisterExternal<NpcInstance, NpcInstance, string>("AI_Output", AI_Output);
            vm.RegisterExternal<NpcInstance>("AI_ProcessInfos", AI_ProcessInfos);
            vm.RegisterExternal<NpcInstance>("AI_StopProcessInfos", AI_StopProcessInfos);
            vm.RegisterExternal<NpcInstance, string>("AI_LookAt", AI_LookAt);
            vm.RegisterExternal<NpcInstance, NpcInstance>("AI_LookAtNPC", AI_LookAtNPC);
            vm.RegisterExternal<NpcInstance>("AI_StopLookAt", AI_StopLookAt);
            vm.RegisterExternal<NpcInstance>("AI_ContinueRoutine", AI_ContinueRoutine);
            vm.RegisterExternal<NpcInstance, NpcInstance>("AI_TurnToNPC", AI_TurnToNPC);
            vm.RegisterExternal<NpcInstance, string, int>("AI_PlayAniBS", AI_PlayAniBS);
            vm.RegisterExternal<NpcInstance>("AI_UnequipArmor", AI_UnequipArmor);
            vm.RegisterExternal<NpcInstance, NpcInstance, string>("AI_OutputSVM", AI_OutputSVM);

            // Apply Options
            // Doc
            // Helper
            vm.RegisterExternal<int, int>("Hlp_Random", Hlp_Random);
            vm.RegisterExternal<int, string, string>("Hlp_StrCmp", Hlp_StrCmp);
            vm.RegisterExternal<int, ItemInstance, int>("Hlp_IsItem", Hlp_IsItem);
            vm.RegisterExternal<int, ItemInstance>("Hlp_IsValidItem", Hlp_IsValidItem);
            vm.RegisterExternal<int, NpcInstance>("Hlp_IsValidNpc", Hlp_IsValidNpc);
            vm.RegisterExternal<NpcInstance, int>("Hlp_GetNpc", Hlp_GetNpc);
            vm.RegisterExternal<int, DaedalusInstance>("Hlp_GetInstanceID", Hlp_GetInstanceID);

            // Info
            vm.RegisterExternal<int>("InfoManager_HasFinished", InfoManager_HasFinished);
            vm.RegisterExternal<int>("Info_ClearChoices", Info_ClearChoices);
            vm.RegisterExternal<int, string, int>("Info_AddChoice", Info_AddChoice);

            // Log
            vm.RegisterExternal<string, string>("Log_AddEntry", Log_AddEntry);
            vm.RegisterExternal<string, int>("Log_CreateTopic", Log_CreateTopic);
            vm.RegisterExternal<string, int>("Log_SetTopicStatus", Log_SetTopicStatus);

            // Model
            vm.RegisterExternal<NpcInstance, string>("Mdl_SetVisual", Mdl_SetVisual);
            vm.RegisterExternal<NpcInstance, string>("Mdl_ApplyOverlayMds", Mdl_ApplyOverlayMds);
            vm.RegisterExternal<NpcInstance, string, int, int, string, int, int, int>("Mdl_SetVisualBody", Mdl_SetVisualBody);
            vm.RegisterExternal<NpcInstance, float, float, float>("Mdl_SetModelScale", Mdl_SetModelScale);
            vm.RegisterExternal<NpcInstance, float>("Mdl_SetModelFatness", Mdl_SetModelFatness);

            // Mission

            // Mob

            // NPC
            vm.RegisterExternal<NpcInstance, int, int>("Npc_SetTalentValue", Npc_SetTalentValue);
            vm.RegisterExternal<NpcInstance, int, int>("Npc_ChangeAttribute", Npc_ChangeAttribute);
            vm.RegisterExternal<NpcInstance, int>("CreateInvItem", CreateInvItem);
            vm.RegisterExternal<NpcInstance, int, int>("CreateInvItems", CreateInvItems);
            vm.RegisterExternal<NpcInstance, int, int>("Npc_PercEnable", Npc_PercEnable);
            vm.RegisterExternal<NpcInstance, float>("Npc_SetPercTime", Npc_SetPercTime);
            vm.RegisterExternal<int, NpcInstance, NpcInstance>("Npc_GetPermAttitude", Npc_GetPermAttitude);
            vm.RegisterExternal<int, NpcInstance, NpcInstance>("Npc_GetAttitude", Npc_GetAttitude);
            vm.RegisterExternal<NpcInstance, int>("Npc_SetAttitude", Npc_SetAttitude);
            vm.RegisterExternal<NpcInstance, int>("Npc_SetTempAttitude", Npc_SetTempAttitude);
            vm.RegisterExternal<int, NpcInstance>("Npc_GetBodyState", Npc_GetBodyState);
            vm.RegisterExternal<NpcInstance>("Npc_PerceiveAll", Npc_PerceiveAll);
            vm.RegisterExternal<int, NpcInstance, int>("Npc_HasItems", Npc_HasItems);
            vm.RegisterExternal<int, NpcInstance>("Npc_GetStateTime", Npc_GetStateTime);
            vm.RegisterExternal<NpcInstance, int>("Npc_SetStateTime", Npc_SetStateTime);
            vm.RegisterExternal<ItemInstance, NpcInstance>("Npc_GetEquippedArmor", Npc_GetEquippedArmor);
            vm.RegisterExternal<NpcInstance, int, int>("Npc_SetTalentSkill", Npc_SetTalentSkill);
            vm.RegisterExternal<string, NpcInstance>("Npc_GetNearestWP", Npc_GetNearestWP);
            vm.RegisterExternal<int, NpcInstance, string>("Npc_IsOnFP", Npc_IsOnFP);
            vm.RegisterExternal<int, NpcInstance, int>("Npc_WasInState", Npc_WasInState);
            // PxVm.pxVmRegisterExternal(vmPtr, "Npc_GetInvItem", Npc_GetInvItem);
            // PxVm.pxVmRegisterExternal(vmPtr, "Npc_GetInvItemBySlot", Npc_GetInvItemBySlot);
            // PxVm.pxVmRegisterExternal(vmPtr, "Npc_RemoveInvItem", Npc_RemoveInvItem);
            // PxVm.pxVmRegisterExternal(vmPtr, "Npc_RemoveInvItems", Npc_RemoveInvItems);
            vm.RegisterExternal<NpcInstance, int>("EquipItem", EquipItem);
            vm.RegisterExternal<int, NpcInstance, NpcInstance>("Npc_GetDistToNpc", Npc_GetDistToNpc);
            vm.RegisterExternal<int, NpcInstance>("Npc_HasEquippedArmor", Npc_HasEquippedArmor);
            vm.RegisterExternal<ItemInstance, NpcInstance>("Npc_GetEquippedMeleeWeapon", Npc_GetEquippedMeleeWeapon);
            vm.RegisterExternal<int, NpcInstance>("Npc_HasEquippedMeleeWeapon", Npc_HasEquippedMeleeWeapon);
            vm.RegisterExternal<ItemInstance, NpcInstance>("Npc_GetEquippedRangedWeapon", Npc_GetEquippedRangedWeapon);
            vm.RegisterExternal<int, NpcInstance>("Npc_HasEquippedRangedWeapon", Npc_HasEquippedRangedWeapon);
            vm.RegisterExternal<int, NpcInstance, string>("Npc_GetDistToWP", Npc_GetDistToWP);
            vm.RegisterExternal<NpcInstance, int>("Npc_PercDisable", Npc_PercDisable);
            vm.RegisterExternal<int, NpcInstance, NpcInstance>("Npc_CanSeeNpc", Npc_CanSeeNpc);
            vm.RegisterExternal<int, NpcInstance, NpcInstance>("Npc_CanSeeNpcFreeLOS", Npc_CanSeeNpcFreeLOS);
            vm.RegisterExternal<NpcInstance>("Npc_ClearAiQueue", Npc_ClearAiQueue);
            // vm.RegisterExternal<NpcInstance>("Npc_ClearInventory", Npc_ClearInventory);
            vm.RegisterExternal<string, NpcInstance>("Npc_GetNextWp", Npc_GetNextWp);
            // vm.RegisterExternal<int, NpcInstance, int>("Npc_GetTalentSkill", Npc_GetTalentSkill);
            vm.RegisterExternal<int, NpcInstance, int>("Npc_GetTalentValue", Npc_GetTalentValue);
            vm.RegisterExternal<int, NpcInstance, int>("Npc_KnowsInfo", Npc_KnowsInfo);
            vm.RegisterExternal<int, NpcInstance, int>("Npc_CheckInfo", Npc_CheckInfo);
            vm.RegisterExternal<int, NpcInstance>("Npc_IsDead", Npc_IsDead);
            vm.RegisterExternal<int, NpcInstance, int>("Npc_IsInState", Npc_IsInState);
            vm.RegisterExternal<NpcInstance>("Npc_SetToFistMode", Npc_SetToFistMode);
            vm.RegisterExternal<int, NpcInstance, int>("Npc_IsInFightMode", Npc_IsInFightMode);
            vm.RegisterExternal<int, NpcInstance>("Npc_IsPlayer", Npc_IsPlayer);
            vm.RegisterExternal<int, ItemInstance, NpcInstance>("Npc_OwnedByNpc", Npc_OwnedByNpc);
            vm.RegisterExternal<int, NpcInstance>("Npc_GetTarget", Npc_GetTarget);
            vm.RegisterExternal<NpcInstance, NpcInstance>("Npc_SetTarget", Npc_SetTarget);
            vm.RegisterExternal<NpcInstance, int, NpcInstance, NpcInstance>("Npc_SendPassivePerc", Npc_SendPassivePerc);
            vm.RegisterExternal<int, NpcInstance, int>("Npc_SetTrueGuild", Npc_SetTrueGuild);
            vm.RegisterExternal<int, NpcInstance>("Npc_GetTrueGuild", Npc_GetTrueGuild);
            vm.RegisterExternal<NpcInstance, int>("Npc_SetRefuseTalk", Npc_SetRefuseTalk);
            vm.RegisterExternal<int, NpcInstance>("Npc_RefuseTalk", Npc_RefuseTalk);


            // Print
            vm.RegisterExternal<string>("PrintDebug", PrintDebug);
            vm.RegisterExternal<int, string>("PrintDebugCh", PrintDebugCh);
            vm.RegisterExternal<string>("PrintDebugInst", PrintDebugInst);
            vm.RegisterExternal<int, string>("PrintDebugInstCh", PrintDebugInstCh);

            // Sound

            // Day Routine
            vm.RegisterExternal<NpcInstance, int, int, int, int, int, string>("TA_MIN", TA_MIN);
            vm.RegisterExternal<NpcInstance, int, int, int, string>("TA", Ta);
            vm.RegisterExternal<NpcInstance, string>("Npc_ExchangeRoutine", Npc_ExchangeRoutine);

            // World
            vm.RegisterExternal<int, string>("Wld_InsertNpc", Wld_InsertNpc);
            vm.RegisterExternal<int, NpcInstance, string>("Wld_IsFPAvailable", Wld_IsFPAvailable);
            vm.RegisterExternal<int, NpcInstance, string>("Wld_IsMobAvailable", Wld_IsMobAvailable);
            vm.RegisterExternal<int, NpcInstance, int, int, int>("Wld_DetectNpc", Wld_DetectNpc);
            vm.RegisterExternal<int, NpcInstance, int, int, int, int>("Wld_DetectNpcEx", Wld_DetectNpcEx);
            vm.RegisterExternal<int, NpcInstance, string>("Wld_IsNextFPAvailable", Wld_IsNextFPAvailable);
            vm.RegisterExternal<int, int>("Wld_SetTime", Wld_SetTime);
            vm.RegisterExternal("Wld_GetDay", Wld_GetDay);
            vm.RegisterExternal<int, int, int, int, int>("Wld_IsTime", Wld_IsTime);
            vm.RegisterExternal<int, NpcInstance, string>("Wld_GetMobState", Wld_GetMobState);
            vm.RegisterExternal<int, string>("Wld_InsertItem", Wld_InsertItem);
            vm.RegisterExternal<string>("Wld_ExchangeGuildAttitudes", Wld_ExchangeGuildAttitudes);
            vm.RegisterExternal<int, int, int>("Wld_SetGuildAttitude", Wld_SetGuildAttitude);
            vm.RegisterExternal<int, int, int>("Wld_GetGuildAttitude", Wld_GetGuildAttitude);

            // Misc
            vm.RegisterExternal<int, int>("Perc_SetRange", Perc_SetRange);
            vm.RegisterExternal<string, string, string>("ConcatStrings", ConcatStrings);
            vm.RegisterExternal<string, int>("IntToString", IntToString);
            vm.RegisterExternal<string, float>("FloatToString", FloatToString);
            vm.RegisterExternal<int, float>("FloatToInt", FloatToInt);
            vm.RegisterExternal<float, int>("IntToFloat", IntToFloat);
            vm.RegisterExternal<string, string, string, string, int>("IntroduceChapter", IntroduceChapter);
        }


        public static void DefaultExternal(DaedalusVm vm, DaedalusSymbol sym)
        {
            // FIXME: Once GUZ is fully released, we can safely throw an exception as it tells us: The game will not work until you implement this missing function.
            //throw new NotImplementedException("External >" + value + "< not registered but required by DaedalusVM.");
            try
            {
                if (GameData.GothicVm.GlobalSelf == null)
                {
                    Logger.LogWarningEditor($"Method >{sym.Name}< not yet implemented in DaedalusVM.", LogCat.ZenKit);
                }
                else
                {
                    // Add additional log information if existing.
                    var selfUserData = GameData.GothicVm.GlobalSelf.UserData as NpcContainer;
                    var npcName = MultiTypeCache.NpcCache.FirstOrDefault(x => x.Instance == selfUserData.Instance)?.Go
                        ?.transform.parent.name;
                    Logger.LogWarningEditor($"Method >{sym.Name}< not yet implemented in DaedalusVM (called on >{npcName}<).", LogCat.ZenKit);
                }
            }
            catch (Exception)
            {
                Logger.LogErrorEditor("Bug in getting Npc. Fix or delete.", LogCat.ZenKit);
            }
        }


        #region AI

        public static void AI_StandUp(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtAiStandUp(npc);
        }

        public static void AI_StandUpQuick(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtAiStandUp(npc);
        }

        public static void AI_SetWalkMode(NpcInstance npc, int walkMode)
        {
            GameGlobals.NpcAi.ExtAiSetWalkMode(npc, (VmGothicEnums.WalkMode)walkMode);
        }

        public static void AI_AlignToFP(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtAiAlignToFp(npc);
        }

        public static void AI_AlignToWP(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtAiAlignToWp(npc);
        }

        public static void AI_GotoFP(NpcInstance npc, string freePointName)
        {
            GameGlobals.NpcAi.ExtAiGoToFp(npc, freePointName);
        }

        public static void AI_GotoWP(NpcInstance npc, string wayPointName)
        {
            GameGlobals.NpcAi.ExtAiGoToWp(npc, wayPointName);
        }

        public static void AI_GotoNpc(NpcInstance self, NpcInstance other)
        {
            GameGlobals.NpcAi.ExtAiGoToNpc(self, other);
        }

        public static void AI_PlayAni(NpcInstance npc, string name)
        {
            GameGlobals.NpcAi.ExtAiPlayAni(npc, name);
        }

        public static void AI_StartState(NpcInstance npc, int function, int stateBehaviour, string wayPointName)
        {
            GameGlobals.NpcAi.ExtAiStartState(npc, function, Convert.ToBoolean(stateBehaviour), wayPointName);
        }

        public static void AI_UseItemToState(NpcInstance npc, int itemId, int expectedInventoryCount)
        {
            GameGlobals.NpcAi.ExtAiUseItemToState(npc, itemId, expectedInventoryCount);
        }

        public static void AI_Wait(NpcInstance npc, float seconds)
        {
            GameGlobals.NpcAi.ExtAiWait(npc, seconds);
        }

        public static void AI_WaitMs(NpcInstance npc, int miliseconds)
        {
            GameGlobals.NpcAi.ExtAiWait(npc, miliseconds / 1000f);
        }

        public static int AI_UseMob(NpcInstance npc, string target, int state)
        {
            GameGlobals.NpcAi.ExtAiUseMob(npc, target, state);

            // Hint: It seems the int value is a bug as no G1 Daedalus usage needs it.
            return 0;
        }

        public static void AI_GoToNextFP(NpcInstance npc, string fpNamePart)
        {
            GameGlobals.NpcAi.ExtAiGoToNextFp(npc, fpNamePart);
        }

        public static void AI_DrawWeapon(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtAiDrawWeapon(npc);
        }

        public static void AI_Output(NpcInstance self, NpcInstance target, string outputName)
        {
            DialogManager.ExtAiOutput(self, target, outputName);
        }

        public static void AI_ProcessInfos(NpcInstance npc)
        {
            DialogManager.ExtAiProcessInfos(npc);
        }

        public static void AI_StopProcessInfos(NpcInstance npc)
        {
            DialogManager.ExtAiStopProcessInfos(npc);
        }

        public static void AI_LookAt(NpcInstance npc, string waypoint)
        {
            GameGlobals.NpcAi.ExtAiLookAt(npc, waypoint);
        }

        public static void AI_LookAtNPC(NpcInstance npc, NpcInstance target)
        {
            GameGlobals.NpcAi.ExtAiLookAtNpc(npc, target);
        }

        public static void AI_StopLookAt(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtAiStopLookAt(npc);
        }

        public static void AI_ContinueRoutine(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtAiContinueRoutine(npc);
        }

        public static void AI_TurnToNPC(NpcInstance npc, NpcInstance target)
        {
            GameGlobals.NpcAi.ExtAiTurnToNpc(npc, target);
        }

        public static void AI_PlayAniBS(NpcInstance npc, string name, int bodyState)
        {
            GameGlobals.NpcAi.ExtAiPlayAniBs(npc, name, bodyState);
        }

        public static void AI_UnequipArmor(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtAiUnequipArmor(npc);
        }

        public static void AI_OutputSVM(NpcInstance npc, NpcInstance target, string svmname)
        {
            DialogManager.ExtAiOutputSvm(npc, target, svmname);
        }

        #endregion

        #region Apply Options

        //

        #endregion

        #region Doc

        //

        #endregion

        #region Helper

        public static int Hlp_Random(int n0)
        {
            return Random.Range(0, n0 - 1);
        }


        public static int Hlp_StrCmp(string s1, string s2)
        {
            return s1 == s2 ? 1 : 0;
        }

        public static int Hlp_IsItem(ItemInstance item, int itemIndexToCheck)
        {
            if (item == null)
            {
                Logger.LogError("Hlp_IsItem called with a null item", LogCat.ZenKit);
                return 0;
            }

            return Convert.ToInt32(item.Index == itemIndexToCheck);
        }

        public static int Hlp_IsValidItem(ItemInstance item)
        {
            return Convert.ToInt32(item != null);
        }

        public static int Hlp_IsValidNpc(NpcInstance npc)
        {
            return Convert.ToInt32(npc != null);
        }

        public static NpcInstance Hlp_GetNpc(int instanceId)
        {
            return GameGlobals.Npcs.ExtHlpGetNpc(instanceId);
        }

        public static int Hlp_GetInstanceID(DaedalusInstance instance)
        {
            if (instance == null)
            {
                return -1;
            }

            return instance.Index;
        }

        #endregion

        #region Info

        public static int InfoManager_HasFinished()
        {
            return Convert.ToInt32(DialogManager.ExtInfoManagerHasFinished());
        }

        public static void Info_ClearChoices(int info)
        {
            DialogManager.ExtInfoClearChoices(info);
        }

        public static void Info_AddChoice(int info, string text, int function)
        {
            DialogManager.ExtInfoAddChoice(info, text, function);
        }

        #endregion

        #region Log

        public static void Log_AddEntry(string topic, string entry)
        {
            GameGlobals.Story.ExtLogAddEntry(topic, entry);
        }

        public static void Log_CreateTopic(string name, int section)
        {
            GameGlobals.Story.ExtLogCreateTopic(name, (SaveTopicSection)section);
        }

        public static void Log_SetTopicStatus(string name, int status)
        {
            GameGlobals.Story.ExtLogSetTopicStatus(name, (SaveTopicStatus)status);
        }

        #endregion

        #region Model

        public static void Mdl_SetVisual(NpcInstance npc, string visual)
        {
            GameGlobals.Npcs.ExtMdlSetVisual(npc, visual);
        }


        public static void Mdl_ApplyOverlayMds(NpcInstance npc, string overlayName)
        {
            GameGlobals.Npcs.ExtApplyOverlayMds(npc, overlayName);
        }

        public struct ExtSetVisualBodyData
        {
            public NpcInstance Npc;
            public string Body;
            public int BodyTexNr;
            public int BodyTexColor;
            public string Head;
            public int HeadTexNr;
            public int TeethTexNr;
            public int Armor;
        }

        public static void Mdl_SetVisualBody(NpcInstance npc, string body, int bodyTexNr, int bodyTexColor, string head,
            int headTexNr, int teethTexNr, int armor)
        {
            GameGlobals.Npcs.ExtSetVisualBody(new ExtSetVisualBodyData
                {
                    Npc = npc,
                    Body = body,
                    BodyTexNr = bodyTexNr,
                    BodyTexColor = bodyTexColor,
                    Head = head,
                    HeadTexNr = headTexNr,
                    TeethTexNr = teethTexNr,
                    Armor = armor
                }
            );
        }


        public static void Mdl_SetModelScale(NpcInstance npc, float x, float y, float z)
        {
            GameGlobals.Npcs.ExtMdlSetModelScale(npc, new Vector3(x, y, z));
        }


        public static void Mdl_SetModelFatness(NpcInstance npc, float fatness)
        {
            GameGlobals.Npcs.ExtSetModelFatness(npc, fatness);
        }

        #endregion

        #region Print

        public static void PrintDebug(string message)
        {
            if (!GameGlobals.Config.Dev.EnableZSpyLogs)
            {
                return;
            }

            Logger.Log($"[zspy]: {message}", LogCat.ZSpy);
        }


        public static void PrintDebugCh(int channel, string message)
        {
            if (!GameGlobals.Config.Dev.EnableZSpyLogs)
            {
                return;
            }

            Logger.Log($"[zspy,{channel}]: {message}", LogCat.ZSpy);
        }


        public static void PrintDebugInst(string message)
        {
            if (!GameGlobals.Config.Dev.EnableZSpyLogs)
            {
                return;
            }

            Logger.Log($"[zspy]: {message}", LogCat.ZSpy);
        }


        public static void PrintDebugInstCh(int channel, string message)
        {
            if (!_enableZSpyLogs || channel > _zSpyChannel)
            {
                return;
            }

            Logger.Log($"[zspy,{channel}]: {message}", LogCat.ZSpy);
        }

        #endregion

        #region Sound

        //

        #endregion

        #region Daily Routine

        //

        #endregion

        #region Mission

        //

        #endregion

        #region Mob

        //

        #endregion

        #region NPC

        public static void Npc_SetTalentValue(NpcInstance npc, int talent, int level)
        {
            GameGlobals.Npcs.ExtNpcSetTalentValue(npc, (VmGothicEnums.Talent)talent, level);
        }

        public static void Npc_ChangeAttribute(NpcInstance npc, int attributeId, int value)
        {
            GameGlobals.Npcs.ExtNpcChangeAttribute(npc, attributeId, value);
        }

        public static void CreateInvItem(NpcInstance npc, int itemId)
        {
            GameGlobals.Npcs.ExtCreateInvItems(npc, itemId, 1);
        }


        public static void CreateInvItems(NpcInstance npc, int itemId, int amount)
        {
            GameGlobals.Npcs.ExtCreateInvItems(npc, itemId, amount);
        }


        public static void Npc_PercEnable(NpcInstance npc, int perception, int function)
        {
            GameGlobals.NpcAi.ExtNpcPerceptionEnable(npc, (VmGothicEnums.PerceptionType)perception, function);
        }


        public static void Npc_SetPercTime(NpcInstance npc, float time)
        {
            GameGlobals.NpcAi.ExtNpcSetPerceptionTime(npc, time);
        }
        
        public static int Npc_GetPermAttitude(NpcInstance self, NpcInstance other)
        {
            return (int)GameGlobals.NpcAi.ExtGetAttitude(self, other);
        }

        public static int Npc_GetAttitude(NpcInstance self, NpcInstance other)
        {
            return (int)GameGlobals.NpcAi.ExtGetAttitude(self, other);
        }
        
        public static void Npc_SetAttitude(NpcInstance self, int attitude)
        {
            GameGlobals.NpcAi.ExtSetAttitude(self, (VmGothicEnums.Attitude)attitude);
        }

        public static void Npc_SetTempAttitude(NpcInstance self, int tempAttitude)
        {
            GameGlobals.NpcAi.ExtSetTempAttitude(self, (VmGothicEnums.Attitude)tempAttitude);
        }

        public static int Npc_GetBodyState(NpcInstance npc)
        {
            return (int)GameGlobals.NpcAi.ExtGetBodyState(npc);
        }


        public static void Npc_PerceiveAll(NpcInstance npc)
        {
            // NOP

            // Gothic loads all the necessary items into memory to reference them later via Wld_DetectNpc() and Wld_DetectItem().
            // But we don't need to pre-load them and can just load the necessary elements when really needed.
        }


        public static int Npc_HasItems(NpcInstance npc, int itemId)
        {
            var count = GameGlobals.Npcs.ExtNpcHasItems(npc, itemId);
            return count;
        }


        public static int Npc_GetStateTime(NpcInstance npc)
        {
            var stateTime = GameGlobals.NpcAi.ExtNpcGetStateTime(npc);
            return stateTime;
        }


        public static void Npc_SetStateTime(NpcInstance npc, int seconds)
        {
            GameGlobals.NpcAi.ExtNpcSetStateTime(npc, seconds);
        }


        public static ItemInstance Npc_GetEquippedArmor(NpcInstance npc)
        {
            return GameGlobals.NpcAi.ExtGetEquippedArmor(npc);
        }


        public static void Npc_SetTalentSkill(NpcInstance npc, int talent, int level)
        {
            GameGlobals.Npcs.ExtNpcSetTalentSkill(npc, (VmGothicEnums.Talent)talent, level);
        }

        public static string Npc_GetNearestWP(NpcInstance npc)
        {
            return GameGlobals.Npcs.ExtGetNearestWayPoint(npc);
        }


        public static int Npc_IsOnFP(NpcInstance npc, string vobNamePart)
        {
            var res = NpcHelper.ExtIsNpcOnFp(npc, vobNamePart);
            return Convert.ToInt32(res);
        }


        public static int Npc_WasInState(NpcInstance npc, int action)
        {
            var result = GameGlobals.NpcAi.ExtNpcWasInState(npc, (uint)action);
            return Convert.ToInt32(result);
        }


        public static void Npc_GetInvItem(IntPtr vmPtr)
        {
            // NpcCreator.ExtGetInvItem();
        }


        public static void Npc_GetInvItemBySlot(IntPtr vmPtr)
        {
            // NpcCreator.ExtGetInvItemBySlot();
        }


        public static void Npc_RemoveInvItem(IntPtr vmPtr)
        {
            // NpcCreator.ExtRemoveInvItem();
        }


        public static void Npc_RemoveInvItems(IntPtr vmPtr)
        {
            // NpcCreator.ExtRemoveInvItems();
        }


        public static void EquipItem(NpcInstance npc, int itemId)
        {
            GameGlobals.Npcs.ExtEquipItem(npc, itemId);
        }


        public static int Npc_GetDistToNpc(NpcInstance npc1, NpcInstance npc2)
        {
            return GameGlobals.NpcAi.ExtNpcGetDistToNpc(npc1, npc2);
        }

        public static int Npc_HasEquippedArmor(NpcInstance npc)
        {
            return GameGlobals.NpcAi.ExtNpcHasEquippedArmor(npc) ? 1 : 0;
        }

        public static ItemInstance Npc_GetEquippedMeleeWeapon(NpcInstance npc)
        {
            return NpcHelper.ExtNpcGetEquippedMeleeWeapon(npc);
        }

        public static int Npc_HasEquippedMeleeWeapon(NpcInstance npc)
        {
            return NpcHelper.ExtNpcHasEquippedMeleeWeapon(npc) ? 1 : 0;
        }

        public static ItemInstance Npc_GetEquippedRangedWeapon(NpcInstance npc)
        {
            return NpcHelper.ExtNpcGetEquippedRangedWeapon(npc);
        }

        public static int Npc_HasEquippedRangedWeapon(NpcInstance npc)
        {
            return NpcHelper.ExtNpcHasEquippedRangedWeapon(npc) ? 1 : 0;
        }

        public static int Npc_GetDistToWP(NpcInstance npc, string waypoint)
        {
            return NpcHelper.ExtNpcGetDistToWp(npc, waypoint);
        }

        public static void Npc_PercDisable(NpcInstance npc, int perception)
        {
            GameGlobals.NpcAi.ExtNpcPerceptionDisable(npc, (VmGothicEnums.PerceptionType)perception);
        }

        public static int Npc_CanSeeNpc(NpcInstance npc, NpcInstance target)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtNpcCanSeeNpc(npc, target, false));
        }

        public static int Npc_CanSeeNpcFreeLOS(NpcInstance npc, NpcInstance target)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtNpcCanSeeNpc(npc, target, true));
        }

        public static void Npc_ClearAiQueue(NpcInstance npc)
        {
            GameGlobals.NpcAi.ExtNpcClearAiQueue(npc);
        }

        public static void Npc_ClearInventory(NpcInstance npc)
        {
            GameGlobals.Npcs.ExtNpcClearInventory(npc);
        }

        public static string Npc_GetNextWp(NpcInstance npc)
        {
            return GameGlobals.Npcs.ExtNpcGetNextWp(npc);
        }

        public static int Npc_GetTalentSkill(NpcInstance npc, int skillId)
        {
            return NpcHelper.ExtNpcGetTalentSkill(npc, skillId);
        }

        public static int Npc_GetTalentValue(NpcInstance npc, int skillId)
        {
            return NpcHelper.ExtNpcGetTalentValue(npc, skillId);
        }

        public static int Npc_KnowsInfo(NpcInstance npc, int infoInstance)
        {
            var res = DialogManager.ExtNpcKnowsInfo(npc, infoInstance);
            return Convert.ToInt32(res);
        }

        public static int Npc_CheckInfo(NpcInstance npc, int important)
        {
            return Convert.ToInt32(DialogManager.ExtCheckInfo(npc, Convert.ToBoolean(important)));
        }
        
        public static int Npc_IsDead(NpcInstance npc)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtNpcIsDead(npc));
        }
        
        public static int Npc_IsInState(NpcInstance npc, int state)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtNpcIsInState(npc, state));
        }

        public static void Npc_SetToFistMode(NpcInstance npc)
        {
            GameGlobals.Npcs.ExtNpcSetToFistMode(npc);
        }

        public static int Npc_IsInFightMode(NpcInstance npc, int fightMode)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtNpcIsInFightMode(npc, (VmGothicEnums.FightMode)fightMode));
        }

        public static int Npc_IsPlayer(NpcInstance npc)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtNpcIsPlayer(npc));
        }

        public static int Npc_OwnedByNpc(ItemInstance item, NpcInstance npc)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtNpcOwnedByNpc(item, npc));
        }
        
        public static int Npc_GetTarget(NpcInstance npc)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtGetTarget(npc));
        }
        
        public static void Npc_SetTarget(NpcInstance npc, NpcInstance target)
        {
            GameGlobals.NpcAi.ExtSetTarget(npc, target);
        }

        public static void Npc_SendPassivePerc(NpcInstance npc, int perc,NpcInstance victim, NpcInstance other)
        {
            GameGlobals.NpcAi.Npc_SendPassivePerc(npc, (VmGothicEnums.PerceptionType)perc, victim, other);
        }        
        
        public static int Npc_SetTrueGuild(NpcInstance npc, int guild)
        {
            GameGlobals.NpcAi.ExtSetTrueGuild(npc, guild);
            return 0;
        }       
        
        public static int Npc_GetTrueGuild(NpcInstance npc)
        {
            return GameGlobals.NpcAi.ExtGetTrueGuild(npc);
        }

        public static void Npc_SetRefuseTalk(NpcInstance npc, int refuseSeconds)
        {
            GameGlobals.NpcAi.ExtSetRefuseTalk(npc, refuseSeconds);
        }

        public static int Npc_RefuseTalk(NpcInstance npc)
        {
            return Convert.ToInt32(GameGlobals.NpcAi.ExtRefuseTalk(npc));
        }

        #endregion

        #region Day Routine

        public static void TA_MIN(NpcInstance npc, int startH, int startM, int stopH, int stopM, int action,
            string waypoint)
        {
            GameGlobals.Npcs.ExtTaMin(npc, startH, startM, stopH, stopM, action, waypoint);
        }

        public static void Ta(NpcInstance npc, int startH, int stopH, int action,
            string waypoint)
        {
            GameGlobals.Npcs.ExtTaMin(npc, startH, 0, stopH, 0, action, waypoint);
        }

        public static void Npc_ExchangeRoutine(NpcInstance self, string routineName)
        {
            GameGlobals.Npcs.ExtNpcExchangeRoutine(self, routineName);
        }

        #endregion

        #region World

        public static void Wld_InsertNpc(int npcInstance, string spawnPoint)
        {
            GameGlobals.Npcs.ExtWldInsertNpc(npcInstance, spawnPoint);
        }


        public static int Wld_IsFPAvailable(NpcInstance npc, string fpName)
        {
            var response = GameGlobals.Npcs.ExtWldIsFpAvailable(npc, fpName);
            return Convert.ToInt32(response);
        }


        public static int Wld_IsMobAvailable(NpcInstance npc, string vobName)
        {
            var res = NpcHelper.ExtIsMobAvailable(npc, vobName);
            return Convert.ToInt32(res);
        }

        public static int Wld_DetectNpc(NpcInstance npc, int npcInstance, int aiState, int guild)
        {
            return Wld_DetectNpcEx(npc, npcInstance, aiState, guild, 1);
        }

        public static int Wld_DetectNpcEx(NpcInstance npc, int npcInstance, int aiState, int guild, int detectPlayer)
        {
            var res = NpcHelper.ExtWldDetectNpcEx(npc, npcInstance, aiState, guild, Convert.ToBoolean(detectPlayer));

            return Convert.ToInt32(res);
        }

        public static int Wld_IsNextFPAvailable(NpcInstance npc, string fpNamePart)
        {
            var result = GameGlobals.Npcs.ExtIsNextFpAvailable(npc, fpNamePart);
            return Convert.ToInt32(result);
        }

        public static void Wld_SetTime(int hour, int minute)
        {
            GameGlobals.Time.SetTime(hour, minute);
        }

        public static int Wld_GetDay()
        {
            return GameGlobals.Time.GetDay();
        }

        public static int Wld_IsTime(int beginHour, int beginMinute, int endHour, int endMinute)
        {
            var begin = new TimeSpan(beginHour, beginMinute, 0);
            var end = new TimeSpan(endHour, endMinute, 0);

            var now = GameGlobals.Time.GetCurrentTime();

            if (begin <= end && begin <= now && now < end)
            {
                return 1;
            }

            if (begin > end && (begin < now || now <= end)) // begin and end span across midnight
            {
                return 1;
            }

            return 0;
        }

        public static int Wld_GetMobState(NpcInstance npc, string scheme)
        {
            return NpcHelper.ExtWldGetMobState(npc, scheme);
        }

        public static void Wld_InsertItem(int itemInstance, string spawnpoint)
        {
            GameGlobals.Vobs.ExtWldInsertItem(itemInstance, spawnpoint);
        }

        public static void Wld_ExchangeGuildAttitudes(string name)
        {
            var guilds = GameData.GothicVm.GetSymbolByName(name);

            if (guilds == null)
                return;

            for (double i = 0, count = GameData.GuildTableSize; i < count; ++i)
            {
                for (var j = 0; j < count; ++j)
                    GameData.GuildAttitudes[(int)(i * count + j)] = guilds.GetInt((ushort)(i * count + j));
            }
        }

        public static void Wld_SetGuildAttitude(int guild1, int attitude, int guild2)
        {
            if (guild1 < 0 || guild2 < 0 || guild1 >= GameData.GuildCount || guild2 >= GameData.GuildCount)
                return;

            GameData.GuildAttitudes[guild1 * GameData.GuildCount + guild2] = attitude;
        }

        public static int Wld_GetGuildAttitude(int guild1, int guild2)
        {
            if (guild1 < 0 || guild2 < 0 || guild1 >= GameData.GuildCount || guild2 >= GameData.GuildCount)
                return 0;

            return GameData.GuildAttitudes[guild1 * GameData.GuildCount + guild2];
        }

        #endregion

        #region Misc

        public static void Perc_SetRange(int perceptionId, int rangeInCm)
        {
            NpcHelper.ExtPErcSetRange(perceptionId, rangeInCm);
        }

        public static string ConcatStrings(string str1, string str2)
        {
            return str1 + str2;
        }


        public static string IntToString(int x)
        {
            return x.ToString();
        }


        public static string FloatToString(float x)
        {
            return x.ToString();
        }


        public static int FloatToInt(float x)
        {
            return (int)x;
        }


        public static float IntToFloat(int x)
        {
            return x;
        }

        public static void IntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            GameGlobals.Story.ExtIntroduceChapter(chapter, text, texture, wav, time);
        }

        #endregion
    }
}
