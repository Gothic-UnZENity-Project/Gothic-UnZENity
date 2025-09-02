using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Models.Config;
using GUZ.Core.World;
using UnityEngine;
using ZenKit;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using WayPoint = GUZ.Core.Models.Vob.WayNet.WayPoint;

namespace GUZ.Core.Creator
{
    public class WayNetService
    {
        public void Create(DeveloperConfig config, WorldContainer world)
        {
            var waynetObj = new GameObject("WayNet");

            SetWayPointCache(world.WayNet);
            CreateWaypoints(waynetObj, world, config.ShowWayPoints);
            CreateDijkstraWaypoints(world.WayNet);
            CreateWaypointEdges(waynetObj, world, config.ShowWayEdges);
        }

        private void SetWayPointCache(IWayNet wayNet)
        {
            GameData.WayPoints.Clear();

            foreach (var wp in wayNet.Points)
            {
                GameData.WayPoints.Add(wp.Name, new WayPoint
                {
                    Name = wp.Name,
                    Position = wp.Position.ToUnityVector(),
                    Direction = wp.Direction.ToUnityVector()
                });
            }
        }

        private void CreateDijkstraWaypoints(IWayNet wayNet)
        {
            CreateDijkstraWaypointEntries(wayNet);
            AttachWaypointPositionToDijkstraEntries();
            CalculateDijkstraNeighbourDistances();
        }

        private void CreateDijkstraWaypointEntries(IWayNet wayNet)
        {
            Dictionary<string, DijkstraWaypoint> dijkstraWaypoints = new();
            var wayEdges = wayNet.Edges;
            var wayPoints = wayNet.Points;

            // Using LINQ to transform wayEdges into DijkstraWaypoints.
            dijkstraWaypoints = wayEdges.SelectMany(edge => new[]
                {
                    // For each edge, create two entries: one for each direction of the edge.
                    // 'a' is the source waypoint, 'b' is the destination waypoint.
                    new { a = wayPoints[edge.A], b = wayPoints[edge.B] },
                    new { a = wayPoints[edge.B], b = wayPoints[edge.A] }
                })
                .GroupBy(x => x.a.Name) // Group the entries by the name of the source waypoint.
                .ToDictionary(g => g.Key, g =>
                    new DijkstraWaypoint(g.Key) // Transform each group into a DijkstraWaypoint.
                    {
                        // The neighbors of the DijkstraWaypoint are the names of the destination waypoints in the group.
                        Neighbors = g.Select(x => x.b.Name).ToList()
                    });

            GameData.DijkstraWaypoints = dijkstraWaypoints;
        }

        private void AttachWaypointPositionToDijkstraEntries()
        {
            foreach (var waypoint in GameData.DijkstraWaypoints)
            {
                var result = GameData.WayPoints.First(i => i.Key == waypoint.Key).Value.Position;
                waypoint.Value.Position = result;
            }
        }

        /// <summary>
        /// Needed for future calculations.
        /// </summary>
        private void CalculateDijkstraNeighbourDistances()
        {
            foreach (var waypoint in GameData.DijkstraWaypoints.Values)
            {
                foreach (var neighbour in waypoint.Neighbors)
                {
                    if (waypoint.DistanceToNeighbors.ContainsKey(neighbour))
                    {
                        continue;
                    }

                    waypoint.DistanceToNeighbors.Add(neighbour,
                        Vector3.Distance(waypoint.Position, GameData.DijkstraWaypoints[neighbour].Position));
                }
            }
        }

        private void CreateWaypoints(GameObject parent, WorldContainer world, bool debugDraw)
        {
            var waypointsObj = new GameObject("WayPoints");
            waypointsObj.SetParent(parent);

            foreach (var waypoint in world.WayNet.Points)
            {
                var wpObject = ResourceLoader.TryGetPrefabObject(PrefabType.WayPoint)!;

                if (debugDraw)
                {
                    var rend = wpObject.AddComponent<MeshRenderer>();
                    var filter = wpObject.AddComponent<MeshFilter>();
                    filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    rend.sharedMaterial = Constants.DebugMaterial;
                }

                wpObject.name = waypoint.Name;
                wpObject.transform.position = waypoint.Position.ToUnityVector();

                wpObject.SetParent(waypointsObj);
            }
        }

        private void CreateWaypointEdges(GameObject parent, WorldContainer world, bool debugDraw)
        {
            var waypointEdgesObj = new GameObject("WayPointEdges");
            waypointEdgesObj.SetParent(parent);

            if (!debugDraw)
            {
                return;
            }

            foreach (var edge in world.WayNet.Edges)
            {
                var startPos = world.WayNet.Points[edge.A].Position.ToUnityVector();
                var endPos = world.WayNet.Points[edge.B].Position.ToUnityVector();
                var lineObj = new GameObject();

                lineObj.AddComponent<LineRenderer>();
                var lr = lineObj.GetComponent<LineRenderer>();
                lr.material = new Material(Constants.ShaderStandard);
                lr.startWidth = 0.1f;
                lr.endWidth = 0.1f;
                lr.SetPosition(0, startPos);
                lr.SetPosition(1, endPos);

                lineObj.name = $"{edge.A}->{edge.B}";
                lineObj.transform.position = startPos;
                lineObj.transform.parent = waypointEdgesObj.transform;
            }
        }
    }
}
