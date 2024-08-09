using GUZ.Core.Manager;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    /// <summary>
    /// Close dialog menu immediately.
    /// </summary>
    public class StopProcessInfos : AbstractAnimationAction
    {
        public StopProcessInfos(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            DialogManager.StopDialog();
            IsFinishedFlag = true;
        }
    }
}
