using System.Collections.Generic;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Manager;
using GUZ.Core.Vob.WayNet;
using GUZ.Core.World;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToWp : AbstractWalkAnimationAction2
    {
        private string Destination => Action.String0;

        private Stack<DijkstraWaypoint> _route;

        public GoToWp(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            var currentWaypoint = Props.CurrentWayPoint ?? WayNetHelper.FindNearestWayPoint(Props.transform.position);
            var destinationWaypoint = (WayPoint)WayNetHelper.GetWayNetPoint(Destination);

            
            /*
             * Two situations, when this action can be skipped:
             * 1. Ai_GoToWp() can get called multiple times until it will loose the WP in between the Ai_StartState() calls. (e.g. ZS_Sleep() -> ZS_StandAround())
             * 2. During spawning (e.g.). As we spawn NPCs onto their current WayPoints, they don't need to walk there from entrance of OC.
             */
            if (destinationWaypoint == null || destinationWaypoint.Name == "" || currentWaypoint.Name == Destination)
            {
                IsFinishedFlag = true;
                return;
            }

            // We need to set the route now to ensure base.Start() can check if NPC is already _on_ the final destination.
            _route = new Stack<DijkstraWaypoint>(WayNetHelper.FindFastestPath(currentWaypoint.Name,
                destinationWaypoint.Name));

            base.Start();
        }

        /// <summary>
        /// Skip animation setting if we're on the final destination right from the start.
        /// </summary>
        protected override void StartWalk()
        {
            if (!IsFinishedFlag)
            {
                base.StartWalk();
            }
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

            if (_route.Count != 0)
            {
                return;
            }

            AnimationEndEventCallback(new SerializableEventEndSignal(""));

            IsFinishedFlag = true;
        }
    }
}
