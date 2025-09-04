﻿using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Adapters.Properties;
using GUZ.Core.Models.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Const;
using GUZ.Core.Core.Logging;
using GUZ.Core.Models.Dialog;
using GUZ.Core.Domain.Npc.Actions;
using GUZ.Core.Domain.Npc.Actions.AnimationActions;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Npc;
using GUZ.Core.Services.World;
using GUZ.Core.Util;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using Logger = GUZ.Core.Core.Logging.Logger;

namespace GUZ.Core.Manager
{
    public class DialogService
    {
        [Inject] private readonly ContextDialogService _contextDialogService;
        [Inject] private readonly NpcService _npcService;
        [Inject] private readonly SaveGameService _saveGameService;

        /// <summary>
        /// TextToSpeech toggle. So that the hero isn't repeating what we said already.
        /// </summary>
        public bool SkipNextOutput;
        
        /// <summary>
        /// Check if NPC has at least one dialog which isn't told and Hero should know about.
        /// important
        ///     - TRUE - check if there's one important dialog untold.
        ///     - FALSE - check if there's one unimportant dialog untold. (unused in G1)
        /// </summary>
        public bool ExtCheckInfo(NpcInstance npc, bool important)
        {
            if (!important)
            {
                // Don't worry. I assume this even makes no sense at all, as also the "END" dialog would always trigger a return true.
                Logger.LogError("Npc_CheckInfo isn't implemented for important=0.", LogCat.Dialog);
            }

            _npcService.SetDialogs(npc.GetUserData());
            
            return TryGetImportant(npc.GetUserData(), out _);
        }

        /// <summary>
        /// initialDialogStarting - We only stop current AI routine if this is the first time the dialog box opens/NPC
        ///     talks important things. Otherwise, the ZS_*_End will get called every time we re-open a dialog in between.
        /// </summary>
        public void StartDialog(NpcContainer npcContainer, bool initialDialogStarting)
        {
            if (initialDialogStarting)
            {
                // Optimization: We expect, that AmbientInfos are assigned before Ai_ProcessInfos() is called.
                _npcService.SetDialogs(npcContainer);

                _contextDialogService.StartDialogInitially();
            }

            GameData.Dialogs.IsInDialog = true;

            // WIP: locking movement 
            GameContext.ContextInteractionService.LockPlayerInPlace();

            // We are already inside a sub-dialog
            if (GameData.Dialogs.CurrentDialog.Options.Any())
            {
                _contextDialogService.FillDialog(npcContainer.Instance, GameData.Dialogs.CurrentDialog.Options);
                _contextDialogService.ShowDialog(npcContainer.Go);
            }
            // There is at least one important entry, the NPC wants to talk to the hero about.
            else if (initialDialogStarting && TryGetImportant(npcContainer, out var infoInstance))
            {
                GameData.Dialogs.CurrentDialog.Instance = infoInstance;
                CallMainInformation(npcContainer, infoInstance);
            }
            else
            {
                var selectableDialogs = new List<InfoInstance>();

                foreach (var dialog in npcContainer.Props.Dialogs)
                {
                    // Dialog is non-permanent and already been told
                    if (dialog.Permanent == 0 && GetInfoState(dialog.Index).Told)
                    {
                        continue;
                    }

                    
                    // TODO - Should be outsourced to some VmManager.Call<int> function which sets and resets values.
                    var oldSelf = GameData.GothicVm.GlobalSelf;
                    GameData.GothicVm.GlobalSelf = npcContainer.Instance;
                    var conditionResult = GameData.GothicVm.Call<int>(dialog.Condition);
                    GameData.GothicVm.GlobalSelf = oldSelf;

                    // Dialog condition is false
                    if (conditionResult == 0)
                    {
                        continue;
                    }
                    
                    // We can now add the dialog
                    selectableDialogs.Add(dialog);
                }

                selectableDialogs = selectableDialogs.OrderBy(d => d.Nr).ToList();
                _contextDialogService.FillDialog(npcContainer.Instance, selectableDialogs);
                _contextDialogService.ShowDialog(npcContainer.Go);
            }
        }

        /// <summary>
        /// If something is important, then call it automatically.
        /// </summary>
        private bool TryGetImportant(NpcContainer npcContainer, out InfoInstance item)
        {
            foreach (var dialog in npcContainer.Props.Dialogs)
            {
                // Dialog is not important.
                if (dialog.Important != 1)
                {
                    continue;
                }

                // Important dialog has already been told.
                if (dialog.Permanent != 1 && GetInfoState(dialog.Index).Told)
                {
                    continue;
                }

                // No dialog condition exists or dialog condition() is false.
                if (dialog.Condition == 0)
                {
                    continue;
                }
                
                // TODO - Should be outsourced to some VmManager.Call<int> function which sets and resets values.
                var oldSelf = GameData.GothicVm.GlobalSelf;
                GameData.GothicVm.GlobalSelf = npcContainer.Instance;
                var conditionResult = GameData.GothicVm.Call<int>(dialog.Condition);
                GameData.GothicVm.GlobalSelf = oldSelf;

                if (conditionResult == 0)
                {
                    continue;
                }

                // Dialog is usable.
                item = dialog;
                return true;
            }

            item = null;
            return false;
        }

        public void ExtAiOutput(NpcInstance self, NpcInstance target, string outputName)
        {
            var isHero = self.Id == 0;
            // Always the NPC we're talking to!
            var speakerId = self.Id;

            var npcTalkingTo = isHero ? target : self;

            npcTalkingTo.GetUserData().Props.AnimationQueue.Enqueue(new Output(
                new AnimationAction(int0: speakerId, string0: outputName),
                npcTalkingTo.GetUserData()));
        }

        /// <summary>
        /// SVM (Standard Voice Module) dialogs are only for NPCs between each other. Not related to Hero dialogs.
        /// </summary>
        public void ExtAiOutputSvm(NpcInstance npc, NpcInstance target, string svmName)
        {
            var npcContainer = GetNpcContainer(npc);

            if (target != null)
            {
                Logger.LogWarning("Ai_OutputSvm() - Handling with target not yet implemented!", LogCat.Dialog);
            }

            npcContainer.Props.AnimationQueue.Enqueue(new OutputSvm(
                new AnimationAction(int0: npcContainer.Instance.Id, string0: svmName),
                npcContainer));
        }

        public bool ExtInfoManagerHasFinished()
        {
            return !GameData.Dialogs.IsInDialog;
        }

        /// <summary>
        /// We update the Unity cached/created elements only.
        /// </summary>
        public void ExtInfoClearChoices(int info)
        {
            GameData.Dialogs.CurrentDialog.Options.Clear();
        }

        public void ExtInfoAddChoice(int info, string text, int function)
        {
            // Check if we need to change current instance as it wasn't cleared before.
            var oldInstance = GameData.Dialogs.CurrentDialog.Instance;

            // First entry of current dialog to add
            if (oldInstance == null)
            {
                GameData.Dialogs.CurrentDialog.Instance = GameData.Dialogs.Instances.First(i => i.Index == info);
                GameData.Dialogs.CurrentDialog.Options.Clear();
            }
            else if (oldInstance.Index != info)
            {
                throw new Exception("Previous Dialog wasn't cleared. Gothic bug? " +
                                    $"Desc={oldInstance.Description}, Npc={oldInstance.Npc}, Info= {oldInstance.Information}");
            }

            // Add new entry
            GameData.Dialogs.CurrentDialog.Options.Add(new DialogOption
            {
                Text = text,
                Function = function
            });
        }

        public void ExtAiProcessInfos(NpcInstance npc)
        {
            var props = GetProperties(npc);

            props.AnimationQueue.Enqueue(new StartProcessInfos(new AnimationAction(bool0: true), npc.GetUserData()));
        }

        public void ExtAiStopProcessInfos(NpcInstance npc)
        {
            var props = GetProperties(npc);

            props.AnimationQueue.Enqueue(new StopProcessInfos(new AnimationAction(), npc.GetUserData()));
        }

        public void MainSelectionClicked(NpcContainer npcContainer, InfoInstance infoInstance)
        {
            CallMainInformation(npcContainer, infoInstance);
        }

        public void SubSelectionClicked(NpcContainer npcContainer, int dialogId)
        {
            CallInformation(npcContainer, dialogId);
        }

        /// <summary>
        /// Skip/Stop current Dialog's .wav entry now.
        /// </summary>
        public void SkipCurrentDialogLine(NpcProperties props)
        {

            if (props.CurrentAction.GetType() == typeof(Output))
            {
                props.CurrentAction.StopImmediately();
            }
        }

        public void StopDialog(NpcContainer npc)
        {
            GameData.Dialogs.CurrentDialog.Instance = null;
            GameData.Dialogs.CurrentDialog.Options.Clear();
            GameData.Dialogs.IsInDialog = false;

            // WIP: unlocking movement
            GameContext.ContextInteractionService.UnlockPlayer();

            _contextDialogService.EndDialog();

            // Hide subtitles from both dialog partners.
            _npcService.GetHeroContainer().PrefabProps.NpcSubtitles.HideSubtitles();
            npc.PrefabProps.NpcSubtitles.HideSubtitles();
        }

        /// <summary>
        /// A C_Info is clicked (main dialog entry)
        /// </summary>
        private void CallMainInformation(NpcContainer npcContainer, InfoInstance infoInstance)
        {
            // Set a new CurrentInstance for potential sub-dialog choices to fetch later.
            GameData.Dialogs.CurrentDialog.Instance = npcContainer.Props.Dialogs
                .First(d => d.Information == infoInstance.Information);

            // Add entry to list of "told" information if it is main element only. (Sub-dialogs will never be reached again as main one (entry point) is already told)
            AddNpcInfoTold(infoInstance.Index);


            // Delegate remaining tasks to general implementation of CallInformation
            CallInformation(npcContainer, infoInstance.Information);
        }

        private void CallInformation(NpcContainer npcContainer, int information)
        {
            _contextDialogService.HideDialog();

            // We always need to set "self" before executing any Daedalus function.
            GameData.GothicVm.GlobalSelf = npcContainer.Instance;
            GameData.GothicVm.GlobalOther = GameData.GothicVm.GlobalHero;

            GameData.GothicVm.Call(information);


            var animationQueue = npcContainer.Props.AnimationQueue;

            // If Daedalus tells us, that the dialog is stopped after this chat (AI_StopProcessInfos), then we're done.
            if (animationQueue.Any(i => i.GetType() == typeof(StopProcessInfos)))
            {
                return;
            }
            // Else we want to have the dialog menu back once all dialog lines are talked.
            else
            {
                animationQueue.Enqueue(new StartProcessInfos(
                    new AnimationAction(int0: information), npcContainer));
            }
        }

        public bool ExtNpcKnowsInfo(NpcInstance npc, int informationIndex)
        {
            var infoInstanceName = GameData.GothicVm.GetSymbolByIndex(informationIndex)!.Name;
            var state = _saveGameService.Save.State;
            SaveInfoState foundInfo = default;
            
            for (var i = 0; i < state.InfoStateCount; i++)
            {
                if (state.GetInfoState(i).Name == infoInstanceName)
                {
                    foundInfo = state.GetInfoState(i);
                    break;
                }
            }
            
            // New entry - Add it now
            if (foundInfo.Name == null)
            {
                foundInfo = new()
                {
                    Name = infoInstanceName,
                    Told = false
                };
                _saveGameService.Save.State.AddInfoState(foundInfo);
            }

            return foundInfo.Told;
        }

        private SaveInfoState GetInfoState(int informationIndex)
        {
            var infoInstanceName = GameData.GothicVm.GetSymbolByIndex(informationIndex)!.Name;
            var state = _saveGameService.Save.State;

            for (var i = 0; i < state.InfoStateCount; i++)
            {
                if (state.GetInfoState(i).Name == infoInstanceName)
                {
                    return state.GetInfoState(i);
                }
            }

            // We safely assume, that if an entry doesn't exist in list, it's simply not yet told.
            return default;
        }

        private void AddNpcInfoTold(int informationIndex)
        {
            var infoInstanceName = GameData.GothicVm.GetSymbolByIndex(informationIndex)!.Name;
            var state = _saveGameService.Save.State;
            
            for (var i = 0; i < state.InfoStateCount; i++)
            {
                if (state.GetInfoState(i).Name == infoInstanceName)
                {
                    state.SetInfoState(i, new SaveInfoState
                    {
                        Name = infoInstanceName,
                        Told = true
                    });
                    return;
                }
            }
            
            Logger.LogWarning($"Couldn't find NpcInfoTold entry for {infoInstanceName}", LogCat.Dialog);
        }

        private GameObject GetGo(NpcInstance npc)
        {
            return npc.GetUserData().Go;
        }

        private NpcContainer GetNpcContainer(NpcInstance npc)
        {
            return npc.GetUserData();
        }

        private NpcProperties GetProperties(NpcInstance npc)
        {
            return npc.GetUserData().Props;
        }
    }
}
