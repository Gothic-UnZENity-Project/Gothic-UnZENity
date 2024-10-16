using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Data;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Npc;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Properties;
using UnityEngine;
using ZenKit.Daedalus;

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
                Debug.LogError("Npc_CheckInfo isn't implemented for important=0.");
            }
            return TryGetImportant(npc.GetUserData().Properties.Dialogs, out _);
        }

        /// <summary>
        /// initialDialogStarting - We only stop current AI routine if this is the first time the dialog box opens/NPC
        ///     talks important things. Otherwise, the ZS_*_End will get called every time we re-open a dialog in between.
        /// </summary>
        public static void StartDialog(GameObject npcGo, NpcProperties properties, bool initialDialogStarting)
        {
            GameData.Dialogs.IsInDialog = true;

            // WIP: locking movement 
            GameContext.InteractionAdapter.LockPlayerInPlace();

            // We are already inside a sub-dialog
            if (GameData.Dialogs.CurrentDialog.Options.Any())
            {
                GameContext.DialogAdapter.FillDialog(properties.NpcInstance, GameData.Dialogs.CurrentDialog.Options);
                GameContext.DialogAdapter.ShowDialog(npcGo);
            }
            // There is at least one important entry, the NPC wants to talk to the hero about.
            else if (initialDialogStarting && TryGetImportant(properties.Dialogs, out var infoInstance))
            {
                GameData.Dialogs.CurrentDialog.Instance = infoInstance;
                properties.Go.GetComponent<AiHandler>().ClearState(true);

                CallMainInformation(properties.NpcInstance, infoInstance);
            }
            else
            {
                if (initialDialogStarting)
                {
                    Debug.Log("StartDialog: initialDialogStarting");
                    properties.Go.GetComponent<AiHandler>().ClearState(false);
                }
                var selectableDialogs = new List<InfoInstance>();

                foreach (var dialog in properties.Dialogs)
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
                GameContext.DialogAdapter.FillDialog(properties.NpcInstance, selectableDialogs);
                GameContext.DialogAdapter.ShowDialog(npcGo);
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
            var npcProps = GetProperties(isHero ? target : self);
            var speakerId = self.Id;

            npcProps.AnimationQueue.Enqueue(new Output(
                new AnimationAction(int0: speakerId, string0: outputName),
                npcProps.Go));
        }

        /// <summary>
        /// SVM (Standard Voice Module) dialogs are only for NPCs between each other. Not related to Hero dialogs.
        /// </summary>
        public static void ExtAiOutputSvm(NpcInstance npc, NpcInstance target, string svmName)
        {
            var props = GetProperties(npc);

            if (target != null)
            {
                Debug.LogError("Ai_OutputSvm() - Handling with target not yet implemented!");
            }

            props.AnimationQueue.Enqueue(new OutputSvm(
                new AnimationAction(int0: props.NpcInstance.Id, string0: svmName),
                props.Go));
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
            StartDialog(GetNpc(npc), GetProperties(npc), true);
        }

        public static void ExtAiStopProcessInfos(NpcInstance npc)
        {
            var props = GetProperties(npc);

            props.AnimationQueue.Enqueue(new StopProcessInfos(new AnimationAction(), props.Go));
        }

        public static void MainSelectionClicked(NpcInstance instance, InfoInstance infoInstance)
        {
            CallMainInformation(instance, infoInstance);
        }

        public static void SubSelectionClicked(NpcInstance instance, int dialogId)
        {
            CallInformation(instance, dialogId);
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

        public static void StopDialog()
        {
            GameData.Dialogs.CurrentDialog.Instance = null;
            GameData.Dialogs.CurrentDialog.Options.Clear();
            GameData.Dialogs.IsInDialog = false;

            // WIP: unlocking movement
            GameContext.InteractionAdapter.UnlockPlayer();

            GameContext.DialogAdapter.HideDialog();
            GameContext.SubtitlesAdapter.HideSubtitles();
        }

        /// <summary>
        /// A C_Info is clicked (main dialog entry)
        /// </summary>
        private static void CallMainInformation(NpcInstance npcInstance, InfoInstance infoInstance)
        {
            var properties = npcInstance.GetUserData().Properties;

            // Set a new CurrentInstance for potential sub-dialog choices to fetch later.
            GameData.Dialogs.CurrentDialog.Instance = properties.Dialogs
                .First(d => d.Information == infoInstance.Information);

            // Add entry to list of "told" information if it is main element only. (Sub-dialogs will never be reached again as main one (entry point) is already told)
            AddNpcInfoTold(infoInstance.Index);


            // Delegate remaining tasks to general implementation of CallInformation
            CallInformation(npcInstance, infoInstance.Information);
        }

        private static void CallInformation(NpcInstance npcInstance, int information)
        {
            var properties = npcInstance.GetUserData().Properties;

            GameContext.DialogAdapter.HideDialog();

            // We always need to set "self" before executing any Daedalus function.
            GameData.GothicVm.GlobalSelf = npcInstance;
            GameData.GothicVm.GlobalOther = GameData.GothicVm.GlobalHero;

            GameData.GothicVm.Call(information);


            var animationQueue = properties.AnimationQueue;

            // If Daedalus tells us, that the dialog is stopped after this chat (AI_StopProcessInfos), then we're done.
            if (animationQueue.Any(i => i.GetType() == typeof(StopProcessInfos)))
            {
                return;
            }
            // Else we want to have the dialog menu back once all dialog lines are talked.
            else
            {
                animationQueue.Enqueue(new StartProcessInfos(
                    new AnimationAction(int0: information), properties.Go));
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

        private static GameObject GetNpc(NpcInstance npc)
        {
            return GetProperties(npc).Go;
        }

        private static NpcProperties GetProperties(NpcInstance npc)
        {
            return npc.GetUserData().Properties;
        }
    }
}
