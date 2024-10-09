using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Properties;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    /// <summary>
    /// Dialog is done. (e.g. END clicked)
    /// Therefore close dialog menu immediately.
    /// </summary>
    public class StopProcessInfos : AbstractAnimationAction
    {
        public StopProcessInfos(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            DialogManager.StopDialog();
            GameGlobals.Story.SwitchChapterIfPending();
            IsFinishedFlag = true;
        }
    }
}
