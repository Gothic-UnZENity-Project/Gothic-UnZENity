using GUZ.Core.Data.Container;
using GUZ.Core.Manager;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class LookAt : AbstractRotateAnimationAction
    {
        private Transform _destinationTransform;
        private string WaypointName => Action.String0;

        public LookAt(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        protected override Quaternion GetRotationDirection()
        {
            var euler = WayNetHelper.GetWayNetPoint(WaypointName).Direction;
            return Quaternion.Euler(euler);
        }
    }
}
