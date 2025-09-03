using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Const;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Debugging
{
    public class DijkstraPathRendererDebug : MonoBehaviour
    {
        [FormerlySerializedAs("debugStart")] public string DebugStart;
        [FormerlySerializedAs("debugEnd")] public string DebugEnd;

        [FormerlySerializedAs("pathDistanceCalculation")]
        public List<string> PathDistanceCalculation;

        private Vector3[] _gizmoWayPoints;
        private GameObject _wayPointsGo;

        /// <summary>
        /// Debug method to draw Gizmo line for selected debugStart -> debugEnd
        /// </summary>
        private void OnValidate()
        {
            // Load rootGo for the first time. Start() would be too early, as world loads later.
            // And we want to have this load only during Editor time, therefore inside OnValidate()
            if (_wayPointsGo == null)
            {
                _wayPointsGo = GameObject.Find("World/Waynet/Waypoints");
            }

            if (GameData.DijkstraWaypoints.TryGetValue(DebugStart, out var startWaypoint) &&
                GameData.DijkstraWaypoints.TryGetValue(DebugEnd, out var endWaypoint))
            {
                LightUpWaypoint(DebugStart, Color.green);
                LightUpWaypoint(DebugEnd, Color.green);

                var watch = Stopwatch.StartNew();
                var path = WayNetHelper.FindFastestPath(DebugStart, DebugEnd);
                watch.Stop();
                Logger.LogEditor($"Path found in {watch.Elapsed.Seconds} seconds.", LogCat.Debug);

                var tempGizmoWayPoints = new List<Vector3>();
                for (var i = 0; i < path.Length - 1; i++)
                {
                    tempGizmoWayPoints.Add(path[i].Position);
                    tempGizmoWayPoints.Add(path[i + 1].Position);
                }

                _gizmoWayPoints = tempGizmoWayPoints.ToArray();

                Logger.LogEditor("Start: " + _gizmoWayPoints.First(), LogCat.Debug);
                Logger.LogEditor("End: " + _gizmoWayPoints.Last(), LogCat.Debug);
            }

            if (PathDistanceCalculation.Count > 0)
            {
                var summarizedDistance = 0.0f;
                for (var i = 0; i < PathDistanceCalculation.Count - 1; i++)
                {
                    var wayPointName1 = PathDistanceCalculation[i];
                    var wayPointName2 = PathDistanceCalculation[i + 1];
                    var wp1 = FindWaypointGo(wayPointName1);
                    var wp2 = FindWaypointGo(wayPointName2);

                    if (wp1 == null || wp2 == null)
                    {
                        summarizedDistance = 0.0f;
                        break;
                    }

                    summarizedDistance += Vector3.Distance(wp1.transform.position, wp2.transform.position);
                }

                if (summarizedDistance > 0.0f)
                {
                    Logger.Log($"Summarized distance: {summarizedDistance}", LogCat.Debug);
                }
            }
        }

        private void OnDrawGizmos()
        {
            // Draw a yellow sphere at the transform's position
            Gizmos.color = Color.green;
            if (_gizmoWayPoints == null)
            {
                return;
            }

            Gizmos.DrawLineList(_gizmoWayPoints);
        }

        private void LightUpWaypoint(string wayPointName, Color color)
        {
            var waypoint = FindWaypointGo(wayPointName);
            if (waypoint == null)
            {
                return;
            }

            var wpRenderer = waypoint.GetComponent<Renderer>();
            if (wpRenderer == null)
            {
                return;
            }

            wpRenderer.material.color = color;
        }

        [CanBeNull]
        private GameObject FindWaypointGo(string wayPointName)
        {
            return _wayPointsGo.FindChildRecursively(wayPointName);
        }
    }
}
