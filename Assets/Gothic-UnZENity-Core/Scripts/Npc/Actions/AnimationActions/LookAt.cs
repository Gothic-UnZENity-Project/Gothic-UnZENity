using GUZ.Core.Manager;
using GUZ.Core.World.WayNet;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class LookAt : AbstractRotateAnimationAction
    {
        private Transform destinationTransform;
        private string waypointName => Action.String0;

        public LookAt(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        { }

        protected override Quaternion GetRotationDirection()
        {
            var euler = WayNetHelper.GetWayNetPoint(waypointName).Direction;
            return Quaternion.Euler(euler);
        }
    }
}
