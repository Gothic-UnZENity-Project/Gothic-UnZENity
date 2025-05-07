using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Data;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Npc;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Properties;
using GUZ.Core.Util;
using UnityEngine;
using ZenKit.Daedalus;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager
{
    public static class DialogManager
    {
        /// <summary>
        /// Check if NPC has at least one dialog which isn't told and Hero should know about.
        /// important
        ///     - TRUE - check if there's one important dialog untold.
        ///     - FALSE - check if there's one unimportant dialog untold. (unused in G1)
        /// </summary>
        public static bool ExtCheckInfo(NpcInstance npc, bool important)
        {
            if (!important)
            {
                // Don't worry. I assume this even makes no sense at all, as also the "END" dialog would always trigger a return true.
                Logger.LogError("Npc_CheckInfo isn't implemented for important=0.", LogCat.Dialog);
            }
            return TryGetImportant(npc.GetUserData2().Props.Dialogs, out _);
        }

        /// <summary>
        /// initialDialogStarting - We only stop current AI routine if this is the first time the dialog box opens/NPC
        ///     talks important things. Otherwise, the ZS_*_End will get called every time we re-open a dialog in between.
        /// </summary>
        public static void StartDialog(NpcContainer npcContainer, bool initialDialogStarting)
        {
            if (initialDialogStarting)
            {
                GameContext.DialogAdapter.StartDialogInitially();
            }

            GameData.Dialogs.IsInDialog = true;

            // WIP: locking movement 
            GameContext.InteractionAdapter.LockPlayerInPlace();

            // We are already inside a sub-dialog
            if (GameData.Dialogs.CurrentDialog.Options.Any())
            {
                GameContext.DialogAdapter.FillDialog(npcContainer.Instance, GameData.Dialogs.CurrentDialog.Options);
                GameContext.DialogAdapter.ShowDialog(npcContainer.Go);
            }
            // There is at least one important entry, the NPC wants to talk to the hero about.
            else if (initialDialogStarting && TryGetImportant(npcContainer.Props.Dialogs, out var infoInstance))
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
                    if (dialog.Permanent == 0 && GameData.KnownDialogInfos.Contains(dialog.Index))
                    {
                        continue;
                    }

                    // Dialog condition is false
                    if (dialog.Condition == 0 || GameData.GothicVm.Call<int>(dialog.Condition) <= 0)
                    {
                        continue;
                    }

                    // We can now add the dialog
                    selectableDialogs.Add(dialog);
                }

                selectableDialogs = selectableDialogs.OrderBy(d => d.Nr).ToList();
                GameContext.DialogAdapter.FillDialog(npcContainer.Instance, selectableDialogs);
                GameContext.DialogAdapter.ShowDialog(npcContainer.Go);
            }
        }

        /// <summary>
        /// If something is important, then call it automatically.
        /// </summary>
        private static bool TryGetImportant(List<InfoInstance> dialogs, out InfoInstance item)
        {
            foreach (var dialog in dialogs)
            {
                // Dialog is not important.
                if (dialog.Important != 1)
                {
                    continue;
                }

                // Important dialog has already been told.
                if (dialog.Permanent != 1 && GameData.KnownDialogInfos.Contains(dialog.Index))
                {
                    continue;
                }

                // No dialog condition exists or dialog condition() is false.
                if (dialog.Condition == 0 || GameData.GothicVm.Call<int>(dialog.Condition) == 0)
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

        public static void ExtAiOutput(NpcInstance self, NpcInstance target, string outputName)
        {
            var isHero = self.Id == 0;
            // Always the NPC we're talking to!
            var speakerId = self.Id;

            var npcTalkingTo = isHero ? target : self;

            npcTalkingTo.GetUserData2().Props.AnimationQueue.Enqueue(new Output(
                new AnimationAction(int0: speakerId, string0: outputName),
                npcTalkingTo.GetUserData2()));
        }

        /// <summary>
        /// SVM (Standard Voice Module) dialogs are only for NPCs between each other. Not related to Hero dialogs.
        /// </summary>
        public static void ExtAiOutputSvm(NpcInstance npc, NpcInstance target, string svmName)
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

        public static bool ExtInfoManagerHasFinished()
        {
            return !GameData.Dialogs.IsInDialog;
        }

        /// <summary>
        /// We update the Unity cached/created elements only.
        /// </summary>
        public static void ExtInfoClearChoices(int info)
        {
            GameData.Dialogs.CurrentDialog.Options.Clear();
        }

        public static void ExtInfoAddChoice(int info, string text, int function)
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

        public static void ExtAiProcessInfos(NpcInstance npc)
        {
            var props = GetProperties(npc);

            props.AnimationQueue.Enqueue(new StartProcessInfos(new AnimationAction(bool0: true), npc.GetUserData2()));
        }

        public static void ExtAiStopProcessInfos(NpcInstance npc)
        {
            var props = GetProperties(npc);

            props.AnimationQueue.Enqueue(new StopProcessInfos(new AnimationAction(), npc.GetUserData2()));
        }

        public static void MainSelectionClicked(NpcContainer npcContainer, InfoInstance infoInstance)
        {
            CallMainInformation(npcContainer, infoInstance);
        }

        public static void SubSelectionClicked(NpcContainer npcContainer, int dialogId)
        {
            CallInformation(npcContainer, dialogId);
        }

        /// <summary>
        /// Skip/Stop current Dialog's .wav entry now.
        /// </summary>
        public static void SkipCurrentDialogLine(NpcProperties props)
        {

            if (props.CurrentAction.GetType() == typeof(Output))
            {
                props.CurrentAction.StopImmediately();
            }
        }

        public static void StopDialog(NpcContainer npc)
        {
            GameData.Dialogs.CurrentDialog.Instance = null;
            GameData.Dialogs.CurrentDialog.Options.Clear();
            GameData.Dialogs.IsInDialog = false;

            // WIP: unlocking movement
            GameContext.InteractionAdapter.UnlockPlayer();

            GameContext.DialogAdapter.EndDialog();

            // Hide subtitles from both dialog partners.
            GameGlobals.Npcs.GetHeroContainer().PrefabProps.NpcSubtitles.HideSubtitles();
            npc.PrefabProps.NpcSubtitles.HideSubtitles();
        }

        /// <summary>
        /// A C_Info is clicked (main dialog entry)
        /// </summary>
        private static void CallMainInformation(NpcContainer npcContainer, InfoInstance infoInstance)
        {
            // Set a new CurrentInstance for potential sub-dialog choices to fetch later.
            GameData.Dialogs.CurrentDialog.Instance = npcContainer.Props.Dialogs
                .First(d => d.Information == infoInstance.Information);

            // Add entry to list of "told" information if it is main element only. (Sub-dialogs will never be reached again as main one (entry point) is already told)
            AddNpcInfoTold(infoInstance.Index);


            // Delegate remaining tasks to general implementation of CallInformation
            CallInformation(npcContainer, infoInstance.Information);
        }

        private static void CallInformation(NpcContainer npcContainer, int information)
        {
            GameContext.DialogAdapter.HideDialog();

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

        public static bool ExtNpcKnowsInfo(NpcInstance npc, int infoInstance)
        {
            return GameData.KnownDialogInfos.Contains(infoInstance);
        }

        public static void AddNpcInfoTold(int informationIndex)
        {
            GameData.KnownDialogInfos.Add(informationIndex);
        }

        private static GameObject GetGo(NpcInstance npc)
        {
            return npc.GetUserData2().Go;
        }

        private static NpcContainer GetNpcContainer(NpcInstance npc)
        {
            return npc.GetUserData2();
        }

        private static NpcProperties GetProperties(NpcInstance npc)
        {
            return npc.GetUserData2().Props;
        }
    }
}
