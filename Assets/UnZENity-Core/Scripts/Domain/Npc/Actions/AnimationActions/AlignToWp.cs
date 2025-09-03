using System;
using GUZ.Core.Data.Container;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using UnityEngine;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    public class AlignToWp : AbstractRotateAnimationAction
    {
        public AlignToWp(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        protected override Quaternion GetRotationDirection()
        {
            try
            {
                var currentWaypoint = Props.CurrentWayPoint ?? WayNetHelper.FindNearestWayPoint(PrefabProps.Bip01.position);

                return Quaternion.Euler(currentWaypoint.Direction);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString(), LogCat.Ai);
                return Quaternion.identity;
            }
        }
    }
}
