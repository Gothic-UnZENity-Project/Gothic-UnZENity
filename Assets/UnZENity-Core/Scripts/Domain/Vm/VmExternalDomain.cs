using System;
using System.Linq;
using System.Numerics;
using GUZ.Core.Models.Container;
using GUZ.Core.Const;
using GUZ.Core.Core.Logging;
using GUZ.Core.Manager;
using GUZ.Core.Models.Vm;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Npc;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Services.World;
using GUZ.Core.Util;
using Reflex.Attributes;
using ZenKit;
using ZenKit.Daedalus;
using Logger = GUZ.Core.Core.Logging.Logger;
using Random = UnityEngine.Random;

namespace GUZ.Core.Domain.Vm
{
    /// <summary>
    /// Contains basic methods only available in Gothic Daedalus module.
    /// </summary>
    public class VmExternalDomain
    {
        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly DialogService _dialogService;
        [Inject] private readonly MultiTypeCacheService _multiTypeCacheService;
        [Inject] private readonly NpcHelperService _npcHelperService;
        [Inject] private readonly NpcService _npcService;
        [Inject] private readonly NpcAiService _npcAiService;
        [Inject] private readonly NpcRoutineService _npcRoutineService;
        [Inject] private readonly GameTimeService _gameTimeService;
        [Inject] private readonly StoryService _storyService;
        [Inject] private readonly VobService _vobService;


        private bool _enableZSpyLogs;
        private int _zSpyChannel;


        public void RegisterExternals()
        {
            _enableZSpyLogs = _configService.Dev.EnableZSpyLogs;
            _zSpyChannel = _configService.Dev.ZSpyChannel;


            var vm = GameData.GothicVm;
            vm.RegisterExternalDefault(DefaultExternal);

            // AI
            vm.RegisterExternal<NpcInstance>("Ai_Attack", Ai_Attack);
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
            vm.RegisterExternal<NpcInstance, int>("Npc_SetToFightMode", Npc_SetToFightMode);
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
            vm.RegisterExternal<int, int, string, int>("Wld_SetObjectRoutine", Wld_SetObjectRoutine);
            vm.RegisterExternal<int, int, string, int>("Wld_SetMobRoutine", Wld_SetMobRoutine);
            vm.RegisterExternal<string, int>("Wld_AssignRoomToGuild", Wld_AssignRoomToGuild);
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


        public void DefaultExternal(DaedalusVm vm, DaedalusSymbol sym)
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
                    var npcName = _multiTypeCacheService.NpcCache.FirstOrDefault(x => x.Instance == selfUserData.Instance)?.Go
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

        public void Ai_Attack(NpcInstance npc)
        {
            _npcAiService.ExtAttack(npc);
        }

        public void AI_StandUp(NpcInstance npc)
        {
            _npcAiService.ExtAiStandUp(npc);
        }

        public void AI_StandUpQuick(NpcInstance npc)
        {
            _npcAiService.ExtAiStandUp(npc);
        }

        public void AI_SetWalkMode(NpcInstance npc, int walkMode)
        {
            _npcAiService.ExtAiSetWalkMode(npc, (VmGothicEnums.WalkMode)walkMode);
        }

        public void AI_AlignToFP(NpcInstance npc)
        {
            _npcAiService.ExtAiAlignToFp(npc);
        }

        public void AI_AlignToWP(NpcInstance npc)
        {
            _npcAiService.ExtAiAlignToWp(npc);
        }

        public void AI_GotoFP(NpcInstance npc, string freePointName)
        {
            _npcAiService.ExtAiGoToFp(npc, freePointName);
        }

        public void AI_GotoWP(NpcInstance npc, string wayPointName)
        {
            _npcAiService.ExtAiGoToWp(npc, wayPointName);
        }

        public void AI_GotoNpc(NpcInstance self, NpcInstance other)
        {
            _npcAiService.ExtAiGoToNpc(self, other);
        }

        public void AI_PlayAni(NpcInstance npc, string name)
        {
            _npcAiService.ExtAiPlayAni(npc, name);
        }

        public void AI_StartState(NpcInstance npc, int function, int stateBehaviour, string wayPointName)
        {
            _npcAiService.ExtAiStartState(npc, function, Convert.ToBoolean(stateBehaviour), wayPointName);
        }

        public void AI_UseItemToState(NpcInstance npc, int itemId, int expectedInventoryCount)
        {
            _npcAiService.ExtAiUseItemToState(npc, itemId, expectedInventoryCount);
        }

        public void AI_Wait(NpcInstance npc, float seconds)
        {
            _npcAiService.ExtAiWait(npc, seconds);
        }

        public void AI_WaitMs(NpcInstance npc, int miliseconds)
        {
            _npcAiService.ExtAiWait(npc, miliseconds / 1000f);
        }

        public int AI_UseMob(NpcInstance npc, string target, int state)
        {
            _npcAiService.ExtAiUseMob(npc, target, state);

            // Hint: It seems the int value is a bug as no G1 Daedalus usage needs it.
            return 0;
        }

        public void AI_GoToNextFP(NpcInstance npc, string fpNamePart)
        {
            _npcAiService.ExtAiGoToNextFp(npc, fpNamePart);
        }

        public void AI_DrawWeapon(NpcInstance npc)
        {
            _npcAiService.ExtAiDrawWeapon(npc);
        }

        public void AI_Output(NpcInstance self, NpcInstance target, string outputName)
        {
            _dialogService.ExtAiOutput(self, target, outputName);
        }

        public void AI_ProcessInfos(NpcInstance npc)
        {
            _dialogService.ExtAiProcessInfos(npc);
        }

        public void AI_StopProcessInfos(NpcInstance npc)
        {
            _dialogService.ExtAiStopProcessInfos(npc);
        }

        public void AI_LookAt(NpcInstance npc, string waypoint)
        {
            _npcAiService.ExtAiLookAt(npc, waypoint);
        }

        public void AI_LookAtNPC(NpcInstance npc, NpcInstance target)
        {
            _npcAiService.ExtAiLookAtNpc(npc, target);
        }

        public void AI_StopLookAt(NpcInstance npc)
        {
            _npcAiService.ExtAiStopLookAt(npc);
        }

        public void AI_ContinueRoutine(NpcInstance npc)
        {
            _npcAiService.ExtAiContinueRoutine(npc);
        }

        public void AI_TurnToNPC(NpcInstance npc, NpcInstance target)
        {
            _npcAiService.ExtAiTurnToNpc(npc, target);
        }

        public void AI_PlayAniBS(NpcInstance npc, string name, int bodyState)
        {
            _npcAiService.ExtAiPlayAniBs(npc, name, bodyState);
        }

        public void AI_UnequipArmor(NpcInstance npc)
        {
            _npcAiService.ExtAiUnequipArmor(npc);
        }

        public void AI_OutputSVM(NpcInstance npc, NpcInstance target, string svmname)
        {
            _dialogService.ExtAiOutputSvm(npc, target, svmname);
        }

        #endregion

        #region Apply Options

        //

        #endregion

        #region Doc

        //

        #endregion

        #region Helper

        public int Hlp_Random(int n0)
        {
            return Random.Range(0, n0 - 1);
        }


        public int Hlp_StrCmp(string s1, string s2)
        {
            return s1 == s2 ? 1 : 0;
        }

        public int Hlp_IsItem(ItemInstance item, int itemIndexToCheck)
        {
            if (item == null)
            {
                Logger.LogError("Hlp_IsItem called with a null item", LogCat.ZenKit);
                return 0;
            }

            return Convert.ToInt32(item.Index == itemIndexToCheck);
        }

        public int Hlp_IsValidItem(ItemInstance item)
        {
            return Convert.ToInt32(item != null);
        }

        public int Hlp_IsValidNpc(NpcInstance npc)
        {
            return Convert.ToInt32(npc != null);
        }

        public NpcInstance Hlp_GetNpc(int instanceId)
        {
            return _npcService.ExtHlpGetNpc(instanceId);
        }

        public int Hlp_GetInstanceID(DaedalusInstance instance)
        {
            if (instance == null)
            {
                return -1;
            }

            return instance.Index;
        }

        #endregion

        #region Info

        public int InfoManager_HasFinished()
        {
            return Convert.ToInt32(_dialogService.ExtInfoManagerHasFinished());
        }

        public void Info_ClearChoices(int info)
        {
            _dialogService.ExtInfoClearChoices(info);
        }

        public void Info_AddChoice(int info, string text, int function)
        {
            _dialogService.ExtInfoAddChoice(info, text, function);
        }

        #endregion

        #region Log

        public void Log_AddEntry(string topic, string entry)
        {
            _storyService.ExtLogAddEntry(topic, entry);
        }

        public void Log_CreateTopic(string name, int section)
        {
            _storyService.ExtLogCreateTopic(name, (SaveTopicSection)section);
        }

        public void Log_SetTopicStatus(string name, int status)
        {
            _storyService.ExtLogSetTopicStatus(name, (SaveTopicStatus)status);
        }

        #endregion

        #region Model

        public void Mdl_SetVisual(NpcInstance npc, string visual)
        {
            _npcService.ExtMdlSetVisual(npc, visual);
        }


        public void Mdl_ApplyOverlayMds(NpcInstance npc, string overlayName)
        {
            _npcService.ExtApplyOverlayMds(npc, overlayName);
        }

        public void Mdl_SetVisualBody(NpcInstance npc, string body, int bodyTexNr, int bodyTexColor, string head,
            int headTexNr, int teethTexNr, int armor)
        {
            _npcService.ExtSetVisualBody(new ExtSetVisualBodyData
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


        public void Mdl_SetModelScale(NpcInstance npc, float x, float y, float z)
        {
            _npcService.ExtMdlSetModelScale(npc, new Vector3(x, y, z));
        }


        public void Mdl_SetModelFatness(NpcInstance npc, float fatness)
        {
            _npcService.ExtSetModelFatness(npc, fatness);
        }

        #endregion

        #region Print

        public void PrintDebug(string message)
        {
            if (!_configService.Dev.EnableZSpyLogs)
            {
                return;
            }

            Logger.Log($"[zspy]: {message}", LogCat.ZSpy);
        }


        public void PrintDebugCh(int channel, string message)
        {
            if (!_configService.Dev.EnableZSpyLogs)
            {
                return;
            }

            Logger.Log($"[zspy,{channel}]: {message}", LogCat.ZSpy);
        }


        public void PrintDebugInst(string message)
        {
            if (!_configService.Dev.EnableZSpyLogs)
            {
                return;
            }

            Logger.Log($"[zspy]: {message}", LogCat.ZSpy);
        }


        public void PrintDebugInstCh(int channel, string message)
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

        public void Npc_SetTalentValue(NpcInstance npc, int talent, int level)
        {
            _npcService.ExtNpcSetTalentValue(npc, (VmGothicEnums.Talent)talent, level);
        }

        public void Npc_ChangeAttribute(NpcInstance npc, int attributeId, int value)
        {
            _npcService.ExtNpcChangeAttribute(npc, attributeId, value);
        }

        public void CreateInvItem(NpcInstance npc, int itemId)
        {
            _npcService.ExtCreateInvItems(npc, itemId, 1);
        }


        public void CreateInvItems(NpcInstance npc, int itemId, int amount)
        {
            _npcService.ExtCreateInvItems(npc, itemId, amount);
        }


        public void Npc_PercEnable(NpcInstance npc, int perception, int function)
        {
            _npcAiService.ExtNpcPerceptionEnable(npc, (VmGothicEnums.PerceptionType)perception, function);
        }


        public void Npc_SetPercTime(NpcInstance npc, float time)
        {
            _npcAiService.ExtNpcSetPerceptionTime(npc, time);
        }

        public int Npc_GetPermAttitude(NpcInstance self, NpcInstance other)
        {
            return (int)_npcAiService.ExtGetAttitude(self, other);
        }

        public int Npc_GetAttitude(NpcInstance self, NpcInstance other)
        {
            return (int)_npcAiService.ExtGetAttitude(self, other);
        }

        public void Npc_SetAttitude(NpcInstance self, int attitude)
        {
            _npcAiService.ExtSetAttitude(self, (VmGothicEnums.Attitude)attitude);
        }

        public void Npc_SetTempAttitude(NpcInstance self, int tempAttitude)
        {
            _npcAiService.ExtSetTempAttitude(self, (VmGothicEnums.Attitude)tempAttitude);
        }

        public int Npc_GetBodyState(NpcInstance npc)
        {
            return (int)_npcAiService.ExtGetBodyState(npc);
        }


        public void Npc_PerceiveAll(NpcInstance npc)
        {
            // NOP

            // Gothic loads all the necessary items into memory to reference them later via Wld_DetectNpc() and Wld_DetectItem().
            // But we don't need to pre-load them and can just load the necessary elements when really needed.
        }


        public int Npc_HasItems(NpcInstance npc, int itemId)
        {
            var count = _npcService.ExtNpcHasItems(npc, itemId);
            return count;
        }


        public int Npc_GetStateTime(NpcInstance npc)
        {
            var stateTime = _npcAiService.ExtNpcGetStateTime(npc);
            return stateTime;
        }


        public void Npc_SetStateTime(NpcInstance npc, int seconds)
        {
            _npcAiService.ExtNpcSetStateTime(npc, seconds);
        }


        public ItemInstance Npc_GetEquippedArmor(NpcInstance npc)
        {
            return _npcAiService.ExtGetEquippedArmor(npc);
        }


        public void Npc_SetTalentSkill(NpcInstance npc, int talent, int level)
        {
            _npcService.ExtNpcSetTalentSkill(npc, (VmGothicEnums.Talent)talent, level);
        }

        public string Npc_GetNearestWP(NpcInstance npc)
        {
            return _npcService.ExtGetNearestWayPoint(npc);
        }


        public int Npc_IsOnFP(NpcInstance npc, string vobNamePart)
        {
            var res = _npcHelperService.ExtIsNpcOnFp(npc, vobNamePart);
            return Convert.ToInt32(res);
        }


        public int Npc_WasInState(NpcInstance npc, int action)
        {
            var result = _npcAiService.ExtNpcWasInState(npc, (uint)action);
            return Convert.ToInt32(result);
        }


        public void Npc_GetInvItem(IntPtr vmPtr)
        {
            // NpcCreator.ExtGetInvItem();
        }


        public void Npc_GetInvItemBySlot(IntPtr vmPtr)
        {
            // NpcCreator.ExtGetInvItemBySlot();
        }


        public void Npc_RemoveInvItem(IntPtr vmPtr)
        {
            // NpcCreator.ExtRemoveInvItem();
        }


        public void Npc_RemoveInvItems(IntPtr vmPtr)
        {
            // NpcCreator.ExtRemoveInvItems();
        }


        public void EquipItem(NpcInstance npc, int itemId)
        {
            _npcService.ExtEquipItem(npc, itemId);
        }


        public int Npc_GetDistToNpc(NpcInstance npc1, NpcInstance npc2)
        {
            return _npcAiService.ExtNpcGetDistToNpc(npc1, npc2);
        }

        public int Npc_HasEquippedArmor(NpcInstance npc)
        {
            return _npcAiService.ExtNpcHasEquippedArmor(npc) ? 1 : 0;
        }

        public ItemInstance Npc_GetEquippedMeleeWeapon(NpcInstance npc)
        {
            return _npcHelperService.ExtNpcGetEquippedMeleeWeapon(npc);
        }

        public int Npc_HasEquippedMeleeWeapon(NpcInstance npc)
        {
            return _npcHelperService.ExtNpcHasEquippedMeleeWeapon(npc) ? 1 : 0;
        }

        public ItemInstance Npc_GetEquippedRangedWeapon(NpcInstance npc)
        {
            return _npcHelperService.ExtNpcGetEquippedRangedWeapon(npc);
        }

        public int Npc_HasEquippedRangedWeapon(NpcInstance npc)
        {
            return _npcHelperService.ExtNpcHasEquippedRangedWeapon(npc) ? 1 : 0;
        }

        public int Npc_GetDistToWP(NpcInstance npc, string waypoint)
        {
            return _npcHelperService.ExtNpcGetDistToWp(npc, waypoint);
        }

        public void Npc_PercDisable(NpcInstance npc, int perception)
        {
            _npcAiService.ExtNpcPerceptionDisable(npc, (VmGothicEnums.PerceptionType)perception);
        }

        public int Npc_CanSeeNpc(NpcInstance npc, NpcInstance target)
        {
            return Convert.ToInt32(_npcAiService.ExtNpcCanSeeNpc(npc, target, false));
        }

        public int Npc_CanSeeNpcFreeLOS(NpcInstance npc, NpcInstance target)
        {
            return Convert.ToInt32(_npcAiService.ExtNpcCanSeeNpc(npc, target, true));
        }

        public void Npc_ClearAiQueue(NpcInstance npc)
        {
            _npcAiService.ExtNpcClearAiQueue(npc);
        }

        public void Npc_ClearInventory(NpcInstance npc)
        {
            _npcService.ExtNpcClearInventory(npc);
        }

        public string Npc_GetNextWp(NpcInstance npc)
        {
            return _npcService.ExtNpcGetNextWp(npc);
        }

        public int Npc_GetTalentSkill(NpcInstance npc, int skillId)
        {
            return _npcHelperService.ExtNpcGetTalentSkill(npc, skillId);
        }

        public int Npc_GetTalentValue(NpcInstance npc, int skillId)
        {
            return _npcHelperService.ExtNpcGetTalentValue(npc, skillId);
        }

        public int Npc_KnowsInfo(NpcInstance npc, int infoInstance)
        {
            var res = _dialogService.ExtNpcKnowsInfo(npc, infoInstance);
            return Convert.ToInt32(res);
        }

        public int Npc_CheckInfo(NpcInstance npc, int important)
        {
            return Convert.ToInt32(_dialogService.ExtCheckInfo(npc, Convert.ToBoolean(important)));
        }

        public int Npc_IsDead(NpcInstance npc)
        {
            return Convert.ToInt32(_npcAiService.ExtNpcIsDead(npc));
        }

        public int Npc_IsInState(NpcInstance npc, int state)
        {
            return Convert.ToInt32(_npcAiService.ExtNpcIsInState(npc, state));
        }

        public void Npc_SetToFistMode(NpcInstance npc)
        {
            _npcService.ExtNpcSetToFistMode(npc);
        }

        public void Npc_SetToFightMode(NpcInstance npc, int itemIndex)
        {
            _npcService.ExtNpcSetToFightMode(npc, itemIndex);
        }

        public int Npc_IsInFightMode(NpcInstance npc, int fightMode)
        {
            return Convert.ToInt32(_npcAiService.ExtNpcIsInFightMode(npc, (VmGothicEnums.FightMode)fightMode));
        }

        public int Npc_IsPlayer(NpcInstance npc)
        {
            return Convert.ToInt32(_npcAiService.ExtNpcIsPlayer(npc));
        }

        public int Npc_OwnedByNpc(ItemInstance item, NpcInstance npc)
        {
            return Convert.ToInt32(_npcAiService.ExtNpcOwnedByNpc(item, npc));
        }

        public int Npc_GetTarget(NpcInstance npc)
        {
            return Convert.ToInt32(_npcAiService.ExtGetTarget(npc));
        }

        public void Npc_SetTarget(NpcInstance npc, NpcInstance target)
        {
            _npcAiService.ExtSetTarget(npc, target);
        }

        public void Npc_SendPassivePerc(NpcInstance npc, int perc,NpcInstance victim, NpcInstance other)
        {
            _npcAiService.Npc_SendPassivePerc(npc, (VmGothicEnums.PerceptionType)perc, victim, other);
        }

        public int Npc_SetTrueGuild(NpcInstance npc, int guild)
        {
            _npcAiService.ExtSetTrueGuild(npc, guild);
            return 0;
        }

        public int Npc_GetTrueGuild(NpcInstance npc)
        {
            return _npcAiService.ExtGetTrueGuild(npc);
        }

        public void Npc_SetRefuseTalk(NpcInstance npc, int refuseSeconds)
        {
            _npcAiService.ExtSetRefuseTalk(npc, refuseSeconds);
        }

        public int Npc_RefuseTalk(NpcInstance npc)
        {
            return Convert.ToInt32(_npcAiService.ExtRefuseTalk(npc));
        }

        #endregion

        #region Day Routine

        public void TA_MIN(NpcInstance npc, int startH, int startM, int stopH, int stopM, int action,
            string waypoint)
        {
            _npcService.ExtTaMin(npc, startH, startM, stopH, stopM, action, waypoint);
        }

        public void Ta(NpcInstance npc, int startH, int stopH, int action,
            string waypoint)
        {
            _npcService.ExtTaMin(npc, startH, 0, stopH, 0, action, waypoint);
        }

        public void Npc_ExchangeRoutine(NpcInstance self, string routineName)
        {
            _npcRoutineService.ExtNpcExchangeRoutine(self, routineName);
        }

        #endregion

        #region World

        public void Wld_InsertNpc(int npcInstance, string spawnPoint)
        {
            _npcService.ExtWldInsertNpc(npcInstance, spawnPoint);
        }


        public int Wld_IsFPAvailable(NpcInstance npc, string fpName)
        {
            var response = _npcService.ExtWldIsFpAvailable(npc, fpName);
            return Convert.ToInt32(response);
        }


        public int Wld_IsMobAvailable(NpcInstance npc, string vobName)
        {
            var res = _npcHelperService.ExtIsMobAvailable(npc, vobName);
            return Convert.ToInt32(res);
        }

        public int Wld_DetectNpc(NpcInstance npc, int npcInstance, int aiState, int guild)
        {
            return Wld_DetectNpcEx(npc, npcInstance, aiState, guild, 1);
        }

        public int Wld_DetectNpcEx(NpcInstance npc, int npcInstance, int aiState, int guild, int detectPlayer)
        {
            var res = _npcHelperService.ExtWldDetectNpcEx(npc, npcInstance, aiState, guild, Convert.ToBoolean(detectPlayer));

            return Convert.ToInt32(res);
        }

        public int Wld_IsNextFPAvailable(NpcInstance npc, string fpNamePart)
        {
            var result = _npcService.ExtIsNextFpAvailable(npc, fpNamePart);
            return Convert.ToInt32(result);
        }

        public void Wld_SetTime(int hour, int minute)
        {
            _gameTimeService.SetTime(hour, minute);
        }

        public int Wld_GetDay()
        {
            return _gameTimeService.GetDay();
        }

        public int Wld_IsTime(int beginHour, int beginMinute, int endHour, int endMinute)
        {
            var begin = new TimeSpan(beginHour, beginMinute, 0);
            var end = new TimeSpan(endHour, endMinute, 0);

            var now = _gameTimeService.GetCurrentTime();

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

        public int Wld_GetMobState(NpcInstance npc, string scheme)
        {
            return _npcHelperService.ExtWldGetMobState(npc, scheme);
        }

        public void Wld_InsertItem(int itemInstance, string spawnpoint)
        {
            _vobService.ExtWldInsertItem(itemInstance, spawnpoint);
        }

        public void Wld_ExchangeGuildAttitudes(string name)
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

        public void Wld_SetGuildAttitude(int guild1, int attitude, int guild2)
        {
            if (guild1 < 0 || guild2 < 0 || guild1 >= GameData.GuildCount || guild2 >= GameData.GuildCount)
                return;

            GameData.GuildAttitudes[guild1 * GameData.GuildCount + guild2] = attitude;
        }

        public void Wld_SetObjectRoutine(int hour, int minute, string name, int status)
        {
            // FIXME - Do more with these ObjectRoutines.
            _vobService.ObjectRoutines.TryAdd(name, new());
            _vobService.ObjectRoutines[name].Add((hour, minute, status));
        }

        public void Wld_SetMobRoutine(int hour, int minute, string name, int status)
        {
            // FIXME - Do more with these MobRoutines.
            _npcService.MobRoutines.TryAdd(name, new());
            _npcService.MobRoutines[name].Add((hour, minute, status));
        }

        private bool _debugWld_AssignRoomToGuildExecuted;
        public void Wld_AssignRoomToGuild(string room, int guild)
        {
            if (!_debugWld_AssignRoomToGuildExecuted)
            {
                Logger.LogWarningEditor($"Method >Wld_AssignRoomToGuild< not yet implemented in DaedalusVM.", LogCat.ZenKit);
                _debugWld_AssignRoomToGuildExecuted = true;
            }
        }

        public int Wld_GetGuildAttitude(int guild1, int guild2)
        {
            if (guild1 < 0 || guild2 < 0 || guild1 >= GameData.GuildCount || guild2 >= GameData.GuildCount)
                return 0;

            return GameData.GuildAttitudes[guild1 * GameData.GuildCount + guild2];
        }

        #endregion

        #region Misc

        public void Perc_SetRange(int perceptionId, int rangeInCm)
        {
            _npcHelperService.ExtPErcSetRange(perceptionId, rangeInCm);
        }

        public string ConcatStrings(string str1, string str2)
        {
            return str1 + str2;
        }


        public string IntToString(int x)
        {
            return x.ToString();
        }


        public string FloatToString(float x)
        {
            return x.ToString();
        }


        public int FloatToInt(float x)
        {
            return (int)x;
        }


        public float IntToFloat(int x)
        {
            return x;
        }

        public void IntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            _storyService.ExtIntroduceChapter(chapter, text, texture, wav, time);
        }

        #endregion
    }
}
