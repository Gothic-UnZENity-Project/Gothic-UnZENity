using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using GUZ.Core.Vob.WayNet;
using GUZ.Core.World;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager
{
    public static class WayNetHelper
    {
        /// <summary>
        /// Check within WayPoints and FreePoints if an entry exists.
        /// </summary>
        /// <param name="pointName"></param>
        /// <returns></returns>
        [CanBeNull]
        public static WayNetPoint GetWayNetPoint(string pointName)
        {
            var wayPoint = GameData.WayPoints
                .FirstOrDefault(item => item.Key.Equals(pointName, StringComparison.OrdinalIgnoreCase))
                .Value;
            if (wayPoint != null)
            {
                return wayPoint;
            }

            var freePoint = GameData.FreePoints
                .FirstOrDefault(pair => pair.Key.Equals(pointName, StringComparison.OrdinalIgnoreCase))
                .Value;
            return freePoint;
        }

        public static List<FreePoint> FindFreePointsWithName(Vector3 lookupPosition, string namePart, float maxDistance)
        {
            var matchingFreePoints = GameData.FreePoints
                .Where(pair => pair.Key.Contains(namePart))
                .Where(pair => Vector3.Distance(lookupPosition, pair.Value.Position) <= maxDistance) // PF is in range
                .OrderBy(pair => Vector3.Distance(lookupPosition, pair.Value.Position)) // order from nearest to farthest
                .Select(pair => pair.Value);

            return matchingFreePoints.ToList();
        }

        public static WayPoint FindNearestWayPoint(Vector3 lookupPosition, bool findSecondNearest = false)
        {
            var wayPoint = GameData.WayPoints
                .OrderBy(pair => Vector3.Distance(pair.Value.Position, lookupPosition))
                .Skip(findSecondNearest ? 1 : 0)
                .FirstOrDefault();

            return wayPoint.Value;
        }

        /// <summary>
        /// npcOnFp -> If the NPC already stands on an FP, we will use this one as return value. (e.g., after ZS_TALK goes back to ZS_SMALLTALK)
        /// </summary>
        [CanBeNull]
        public static FreePoint FindNearestFreePoint(Vector3 lookupPosition, string fpNamePart, FreePoint npcOnFp)
        {
            return GameData.FreePoints
                .Where(pair => pair.Value.Name.ContainsIgnoreCase(fpNamePart) && (pair.Value == npcOnFp || !pair.Value.IsLocked))
                .OrderBy(pair => Vector3.Distance(pair.Value.Position, lookupPosition))
                .Select(pair => pair.Value)
                .FirstOrDefault();
        }


        public static DijkstraWaypoint[] FindFastestPath(string startWaypoint, string endWaypoint)
        {
            // Get the start and end waypoints from the DijkstraWaypoints dictionary
            var startDijkstraWaypoint = GameData.DijkstraWaypoints[startWaypoint];
            var endDijkstraWaypoint = GameData.DijkstraWaypoints[endWaypoint];

            // Initialize the previousNodes dictionary to keep track of the path
            var previousNodes = new Dictionary<string, DijkstraWaypoint>();
            // Initialize the unvisited priority queue to keep track of waypoints to be visited
            var unvisited = new PriorityQueue();

            // For each waypoint in DijkstraWaypoints
            foreach (var waypointX in GameData.DijkstraWaypoints.Values)
            {
                // If the waypoint is the start waypoint, set its SummedDistance to 0
                if (waypointX.Name == startWaypoint)
                {
                    waypointX.SummedDistance = 0;
                }
                // Otherwise, set its SummedDistance to infinity
                else
                {
                    waypointX.SummedDistance = double.MaxValue;
                }

                // Add the waypoint to the unvisited set and set its previous node to null
                unvisited.Enqueue(waypointX, waypointX.SummedDistance);
                // Add the waypoint to the previousNodes dictionary and set its previous node to null
                previousNodes[waypointX.Name] = null;
            }

            while (unvisited.Count > 0)
            {
                var currentWaypoint = unvisited.Dequeue();

                foreach (var neighborName in currentWaypoint.DistanceToNeighbors.Keys)
                {
                    var neighbor = GameData.DijkstraWaypoints[neighborName];
                    var alt = currentWaypoint.SummedDistance + currentWaypoint.DistanceToNeighbors[neighborName];

                    // If a shorter path to the neighbor is found, update its distance and previous node.
                    if (alt < neighbor.SummedDistance)
                    {
                        neighbor.SummedDistance = alt;
                        previousNodes[neighbor.Name] = currentWaypoint;

                        // If the neighbor is in the unvisited set, update its priority.
                        if (unvisited.Contains(neighbor))
                        {
                            unvisited.UpdatePriority(neighbor, alt);
                        }
                        // Otherwise, add it to the unvisited set with the new priority.
                        else
                        {
                            unvisited.Enqueue(neighbor, alt);
                        }
                    }
                }

                // Check if a valid path from Start to End has been found
                var lastChecked = endWaypoint;
                while (lastChecked != null && previousNodes[lastChecked] != null)
                {
                    lastChecked = previousNodes[lastChecked].Name;
                }

                if (lastChecked == startWaypoint)
                {
                    break;
                }
            }

            // Construct the shortest path
            var path = new List<DijkstraWaypoint>();
            var waypoint = endDijkstraWaypoint;
            while (waypoint != null)
            {
                path.Insert(0, waypoint);
                waypoint = previousNodes[waypoint.Name];
            }

            path.Reverse();
            return path.ToArray();
        }


        private class PriorityQueue
        {
            private List<KeyValuePair<DijkstraWaypoint, double>> _data = new();

            public bool Contains(DijkstraWaypoint item)
            {
                // Check if the queue contains the item by comparing the names of the waypoints
                return _data.Select(x => x.Key.Name == item.Name).Count() > 0;
            }

            public void UpdatePriority(DijkstraWaypoint item, double priority)
            {
                // Find the index of the item
                var index = _data.FindIndex(pair => pair.Key.Name == item.Name);
                if (index == -1)
                {
                    // If the item is not in the queue, throw an exception (it should never do that so this is a good way to catch bugs)
                    throw new ArgumentException("Item does not exist in the queue.");
                }

                // Get the old priority of the item
                var oldPriority = _data[index].Value;
                // Update the priority of the item in the queue
                _data[index] = new KeyValuePair<DijkstraWaypoint, double>(item, priority);

                // If the new priority is less than the old priority, sift up
                if (priority < oldPriority)
                {
                    SiftUp(index);
                }
                // If the new priority is greater than the old priority, sift down
                else if (priority > oldPriority)
                {
                    SiftDown(index);
                }
            }

            /// <summary>
            /// "Shift" in the context of a heap data structure refers to the process of adjusting
            /// the position of an element to maintain the heap property. This is done by moving
            /// the element up or down in the heap until the heap property is satisfied.
            /// </summary>
            private void SiftUp(int index)
            {
                var parentIndex = (index - 1) / 2;
                while (index > 0 && _data[index].Value < _data[parentIndex].Value)
                {
                    Swap(index, parentIndex);
                    index = parentIndex;
                    parentIndex = (index - 1) / 2;
                }
            }

            private void SiftDown(int index)
            {
                var leftChildIndex = index * 2 + 1;
                var rightChildIndex = index * 2 + 2;
                var smallestChildIndex = leftChildIndex;

                if (rightChildIndex < _data.Count && _data[rightChildIndex].Value < _data[leftChildIndex].Value)
                {
                    smallestChildIndex = rightChildIndex;
                }

                while (smallestChildIndex < _data.Count && _data[smallestChildIndex].Value < _data[index].Value)
                {
                    Swap(index, smallestChildIndex);
                    index = smallestChildIndex;
                    leftChildIndex = index * 2 + 1;
                    rightChildIndex = index * 2 + 2;
                    smallestChildIndex = leftChildIndex;

                    if (rightChildIndex < _data.Count && _data[rightChildIndex].Value < _data[leftChildIndex].Value)
                    {
                        smallestChildIndex = rightChildIndex;
                    }
                }
            }

            private void Swap(int index1, int index2)
            {
                (_data[index1], _data[index2]) = (_data[index2], _data[index1]);
            }

            public void Enqueue(DijkstraWaypoint waypoint, double priority)
            {
                _data.Add(new KeyValuePair<DijkstraWaypoint, double>(waypoint, priority));
                var currentIndex = _data.Count - 1;

                while (currentIndex > 0)
                {
                    var parentIndex = (currentIndex - 1) / 2;

                    if (_data[currentIndex].Value >= _data[parentIndex].Value)
                    {
                        break;
                    }

                    (_data[currentIndex], _data[parentIndex]) = (_data[parentIndex], _data[currentIndex]);

                    currentIndex = parentIndex;
                }
            }

            public DijkstraWaypoint Dequeue()
            {
                var lastIndex = _data.Count - 1;
                var frontItem = _data[0].Key;
                _data[0] = _data[lastIndex];
                _data.RemoveAt(lastIndex);

                --lastIndex;
                var parentIndex = 0;

                while (true)
                {
                    var leftChildIndex = parentIndex * 2 + 1;
                    if (leftChildIndex > lastIndex)
                    {
                        break;
                    }

                    var rightChildIndex = leftChildIndex + 1;
                    if (rightChildIndex <= lastIndex && _data[rightChildIndex].Value < _data[leftChildIndex].Value)
                    {
                        leftChildIndex = rightChildIndex;
                    }

                    if (_data[parentIndex].Value <= _data[leftChildIndex].Value)
                    {
                        break;
                    }

                    (_data[parentIndex], _data[leftChildIndex]) = (_data[leftChildIndex], _data[parentIndex]);

                    parentIndex = leftChildIndex;
                }

                return frontItem;
            }

            public int Count => _data.Count;

            public void Remove(DijkstraWaypoint waypoint)
            {
                var index = _data.FindIndex(pair => pair.Key.Name == waypoint.Name);
                if (index == -1)
                {
                    //throw new ArgumentException("The specified waypoint is not in the queue.");
                    Logger.Log("The specified waypoint " + waypoint.Name + " is not in the queue.", LogCat.Misc);
                    return;
                }

                _data.RemoveAt(index);
            }
        }
    }
}
