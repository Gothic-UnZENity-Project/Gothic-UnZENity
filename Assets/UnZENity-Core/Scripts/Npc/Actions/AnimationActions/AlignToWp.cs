using System;
using GUZ.Core._Npc2;
using UnityEngine;

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
                Debug.LogError(e);
                return Quaternion.identity;
                ;
            }
        }
    }
}
