using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Manager;
using GUZ.Core.World;
using GUZ.Core.World.WayNet;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToWp : AbstractWalkAnimationAction
    {
        private string destination => Action.String0;

        private Stack<DijkstraWaypoint> route;
            
        public GoToWp(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        { }

        public override void Start()
        {
            base.Start();

            var currentWaypoint = Props.CurrentWayPoint ?? WayNetHelper.FindNearestWayPoint(Props.transform.position);
            var destinationWaypoint = (WayPoint)WayNetHelper.GetWayNetPoint(destination);

            /*
             * 1. AI_StartState() can get called multiple times until it won't share the WP. (e.g. ZS_SLEEP -> ZS_StandAround())
             * 2. Happens (e.g.) during spawning. As we spawn NPCs onto their current WayPoints, they don't need to walk there from entrance of OC.
             */
            if (destinationWaypoint == null || destinationWaypoint.Name == "" || currentWaypoint.Name == destination)
            {
                IsFinishedFlag = true;
                return;
            }

            route = new Stack<DijkstraWaypoint>(WayNetHelper.FindFastestPath(currentWaypoint.Name,
                destinationWaypoint.Name));
        }

        protected override Vector3 GetWalkDestination()
        {
            return route.Peek().Position;
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);

            IsFinishedFlag = false;
        }

        protected override void OnDestinationReached()
        {
            route.Pop();

            if (route.Count == 0)
            {
                AnimationEndEventCallback(new SerializableEventEndSignal(nextAnimation: ""));

                walkState = WalkState.Done;
                IsFinishedFlag = true;
            }
            else
            {
                // A new waypoint is destination, we therefore rotate NPC again.
                walkState = WalkState.WalkAndRotate;
            }
        }
    }
}
