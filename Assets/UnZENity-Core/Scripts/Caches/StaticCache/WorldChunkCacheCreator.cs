using System;
using System.Collections.Generic;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using UnityEngine;
using ZenKit;
using TextureFormat = ZenKit.TextureFormat;

namespace GUZ.Core.Caches.StaticCache
{
    public class WorldChunkCacheCreator
    {
        [Serializable] // For cache saving reasons
        public class WorldChunk
        {
            public List<int> PolygonIds = new();
        }

        public Dictionary<TextureCache.TextureArrayTypes, List<WorldChunk>> MergedChunksByLights;

        private List<Bounds> _stationaryLightBounds;

        public void CalculateWorldChunks(IWorld world, List<Bounds> stationaryLightBounds)
        {
            _stationaryLightBounds = stationaryLightBounds;

            // Hint: We need to cache BspTree, otherwise looping through it will take ages.
            BuildBspTree(world.Mesh, (CachedBspTree)world.BspTree.Cache());
        }

        private void BuildBspTree(IMesh worldMesh, CachedBspTree bspTree)
        {
            var leafNodes = new List<int>();
            CalculateLeafNodes(leafNodes, bspTree, 0);

            var leafsWithPolygons = CalculatePolygonsToLeafNodes(worldMesh, bspTree, leafNodes);

            MergeWorldChunksByLightCount(worldMesh, bspTree, leafsWithPolygons);
        }

        private void CalculateLeafNodes(List<int> leafNodes, CachedBspTree tree, int currentNodeId)
        {
            if (currentNodeId == -1)
            {
                return;
            }

            var node = tree.GetNode(currentNodeId);
            if (IsLeaf(node))
            {
                leafNodes.Add(currentNodeId);
            }
            // The current element is no leaf. We therefore check its children.
            else
            {
                CalculateLeafNodes(leafNodes, tree, node.FrontIndex);
                CalculateLeafNodes(leafNodes, tree, node.BackIndex);
            }
        }

        private bool IsLeaf(BspNode node)
        {
            return node.FrontIndex == -1 && node.BackIndex == -1;
        }

        /// <summary>
        /// Returns NODE_ID => List{POLYGON_ID}
        /// </summary>
        private Dictionary<int, List<int>> CalculatePolygonsToLeafNodes(IMesh mesh, CachedBspTree bspTree, List<int> leafNodes)
        {
            var returnData = new Dictionary<int, List<int>>();

            // Multiple Nodes reference the same polygons. We need to ignore these duplicates (otherwise, we will draw them multiple times with no further use ;-).
            var usedPolygonIndices = new HashSet<int>();

            foreach (var nodeId in leafNodes)
            {
                var currentNodePolygonIds = new List<int>();

                var node = bspTree.GetNode(nodeId);
                for (var polygonId = node.PolygonIndex; polygonId < node.PolygonIndex + node.PolygonCount; polygonId++)
                {
                    var polygonIndexToUse = bspTree.PolygonIndices[polygonId];

                    // Can't be added twice and therefore returns false the second time.
                    if (usedPolygonIndices.Contains(polygonIndexToUse))
                    {
                        continue;
                    }

                    // Different leaf nodes reference the same polygons.
                    // Manually check if polygons have been used to avoid creating overlapping geometry.
                    usedPolygonIndices.Add(polygonIndexToUse);

                    var polygon = mesh.GetPolygon(polygonIndexToUse);

                    // We ignore portals for now.
                    if (polygon.IsPortal)
                    {
                        continue;
                    }

                    currentNodePolygonIds.Add(polygonIndexToUse);
                }

                returnData.Add(nodeId, currentNodePolygonIds);
            }

            return returnData;
        }

        /// <summary>
        /// As a BspTree is ordered (aka left-right elements are next to each other), we assume, that all the leafNode information
        /// is next to each other. Aka if a submesh is "filled" with lights, we close it. And other leafNodes will open up a new mesh.
        ///
        /// Alternatively we would need to create an adjacent method to calculate if Nodes are next to each other.
        /// </summary>
        private void MergeWorldChunksByLightCount(IMesh mesh, CachedBspTree bspTree, Dictionary<int, List<int>> leafNodesWithPolygons)
        {
            var finalPolygonsOpaque = new List<WorldChunk>();
            var finalPolygonsTransparent = new List<WorldChunk>();
            var finalPolygonsWater = new List<WorldChunk>();

            var currentOpaqueChunkPolygons = new WorldChunk();
            var isCurrentNodeWithOpaque = false;
            var currentOpaqueChunkLightsCount = 0;

            var currentTransparentChunkPolygons = new WorldChunk();
            var isCurrentNodeWithTransparent = false;
            var currentTransparentChunkLightsCount = 0;

            var currentWaterChunkPolygons = new WorldChunk();
            var isCurrentNodeWithWater = false;
            var currentWaterChunkLightsCount = 0;

            foreach (var nodeData in leafNodesWithPolygons)
            {
                var node = bspTree.GetNode(nodeData.Key);
                var polygonIds = nodeData.Value;

                var lightsOfChunk = GetLightsInBound(node.BoundingBox);

                // This indicator will later on decide, if a node has a value of this type. If so, we add the light count.
                isCurrentNodeWithOpaque = false;
                isCurrentNodeWithTransparent = false;
                isCurrentNodeWithWater = false;

                foreach (var polygonId in polygonIds)
                {
                    var polygon = mesh.GetPolygon(polygonId);
                    var material = mesh.GetMaterial(polygon.MaterialIndex);
                    var texture = ResourceLoader.TryGetTexture(material.Texture);

                    if (texture == null)
                    {
                        continue;
                    }

                    if (material.Group == MaterialGroup.Water)
                    {
                        // First time we have a Polygon with Water in this node.
                        if (!isCurrentNodeWithWater)
                        {
                            currentWaterChunkLightsCount += lightsOfChunk;
                            isCurrentNodeWithWater = true;
                        }
                        currentWaterChunkPolygons.PolygonIds.Add(polygonId);
                    }
                    else if (texture.Format == TextureFormat.Dxt1)
                    {
                        // First time we have a Polygon with Dxt1 in this node.
                        if (!isCurrentNodeWithOpaque)
                        {
                            currentOpaqueChunkLightsCount += lightsOfChunk;
                            isCurrentNodeWithOpaque = true;
                        }
                        currentOpaqueChunkPolygons.PolygonIds.Add(polygonId);
                    }
                    // aka TextureFormat.R8G8B8A8 or anything else (e.g. DTX3, which will be changed to uncompressed R8G8B8A8 anyways)
                    else
                    {
                        // First time we have a Polygon with R8G8B8A8 in this node.
                        if (!isCurrentNodeWithTransparent)
                        {
                            currentTransparentChunkLightsCount += lightsOfChunk;
                            isCurrentNodeWithTransparent = true;
                        }
                        currentTransparentChunkPolygons.PolygonIds.Add(polygonId);
                    }
                }

                // Close node run
                {
                    if (currentWaterChunkLightsCount > Constants.MaxLightsPerWorldChunk)
                    {
                        if (currentWaterChunkPolygons.PolygonIds.NotNullOrEmpty())
                        {
                            finalPolygonsWater.Add(currentWaterChunkPolygons);
                        }
                        currentWaterChunkPolygons = new WorldChunk();
                        currentWaterChunkLightsCount = 0;
                    }
                    if (currentOpaqueChunkLightsCount > Constants.MaxLightsPerWorldChunk)
                    {
                        if (currentOpaqueChunkPolygons.PolygonIds.NotNullOrEmpty())
                        {
                            finalPolygonsOpaque.Add(currentOpaqueChunkPolygons);
                        }
                        currentOpaqueChunkPolygons = new WorldChunk();
                        currentOpaqueChunkLightsCount = 0;
                    }
                    if (currentTransparentChunkLightsCount > Constants.MaxLightsPerWorldChunk)
                    {
                        if (currentTransparentChunkPolygons.PolygonIds.NotNullOrEmpty())
                        {
                            finalPolygonsTransparent.Add(currentTransparentChunkPolygons);
                        }
                        currentTransparentChunkPolygons = new WorldChunk();
                        currentTransparentChunkLightsCount = 0;
                    }
                }
            }


            // Close whole node run
            {
                if (currentOpaqueChunkPolygons.PolygonIds.NotNullOrEmpty())
                {
                    finalPolygonsOpaque.Add(currentOpaqueChunkPolygons);
                }
                if (currentTransparentChunkPolygons.PolygonIds.NotNullOrEmpty())
                {
                    finalPolygonsTransparent.Add(currentTransparentChunkPolygons);
                }
                if (currentWaterChunkPolygons.PolygonIds.NotNullOrEmpty())
                {
                    finalPolygonsWater.Add(currentWaterChunkPolygons);
                }
            }

            MergedChunksByLights = new()
            {
                { TextureCache.TextureArrayTypes.Opaque, new List<WorldChunk>(finalPolygonsOpaque) },
                { TextureCache.TextureArrayTypes.Transparent, finalPolygonsTransparent },
                { TextureCache.TextureArrayTypes.Water, finalPolygonsWater }
            };
        }

        private int GetLightsInBound(AxisAlignedBoundingBox aabb)
        {
            var returnCount = 0;
            var unityBbox = aabb.ToUnityBounds();

            foreach (var lightBound in _stationaryLightBounds)
            {
                if (lightBound.Intersects(unityBbox))
                {
                    returnCount++;
                }
            }

            return returnCount;
        }
    }
}
