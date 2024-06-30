using System.Collections.Generic;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Manager;
using GUZ.Core.Vob.WayNet;
using GUZ.Core.World;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToWp : AbstractWalkAnimationAction
    {
        private string Destination => Action.String0;

        private Stack<DijkstraWaypoint> _route;

        public GoToWp(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            base.Start();

            var currentWaypoint = Props.CurrentWayPoint ?? WayNetHelper.FindNearestWayPoint(Props.transform.position);
            var destinationWaypoint = (WayPoint)WayNetHelper.GetWayNetPoint(Destination);

            /*
             * 1. AI_StartState() can get called multiple times until it won't share the WP. (e.g. ZS_SLEEP -> ZS_StandAround())
             * 2. Happens (e.g.) during spawning. As we spawn NPCs onto their current WayPoints, they don't need to walk there from entrance of OC.
             */
            if (destinationWaypoint == null || destinationWaypoint.Name == "" || currentWaypoint.Name == Destination)
            {
                IsFinishedFlag = true;
                return;
            }

            _route = new Stack<DijkstraWaypoint>(WayNetHelper.FindFastestPath(currentWaypoint.Name,
                destinationWaypoint.Name));
        }

        protected override Vector3 GetWalkDestination()
        {
            return _route.Peek().Position;
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);

            IsFinishedFlag = false;
        }

        protected override void OnDestinationReached()
        {
            _route.Pop();

            if (_route.Count == 0)
            {
                AnimationEndEventCallback(new SerializableEventEndSignal(""));

                State = WalkState.Done;
                IsFinishedFlag = true;
            }
            else
            {
                // A new waypoint is destination, we therefore rotate NPC again.
                State = WalkState.WalkAndRotate;
            }
        }
    }
}
