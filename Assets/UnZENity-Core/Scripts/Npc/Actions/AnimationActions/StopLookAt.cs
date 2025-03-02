using System.Collections.Generic;
using GUZ.Core._Npc2;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using UnityEngine;
using AnimationCreator = MyBox.Internal.AnimationCreator;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class StopLookAtNpc : AbstractAnimationAction
    {
        public StopLookAtNpc(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        { }

        public override void Start()
        {
            PrefabProps.AnimationHeadHandler.StopLookAt();

            IsFinishedFlag = true;
        }
    }
}
