using System;
using GUZ.Core._Npc2;
using GUZ.Core.Util;
using UnityEngine;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class AlignToWp : AbstractRotateAnimationAction
    {
        public AlignToWp(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        protected override Quaternion GetRotationDirection()
        {
            try
            {
                var euler = Props.CurrentWayPoint.Direction;
                return Quaternion.Euler(euler);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString(), LogCat.Ai);
                return Quaternion.identity;
                ;
            }
        }
    }
}
