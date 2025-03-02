using System.Collections.Generic;
using GUZ.Core._Npc2;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using UnityEngine;
using AnimationCreator = MyBox.Internal.AnimationCreator;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class LookAtNpc : AbstractAnimationAction
    {
        private Transform _otherHead => Action.Instance0.GetUserData2().PrefabProps.Head.transform;


        public LookAtNpc(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        { }

        public override void Start()
        {
            PrefabProps.AnimationHeadHandler.StartLookAt(_otherHead);

            IsFinishedFlag = true;
        }
    }
}
