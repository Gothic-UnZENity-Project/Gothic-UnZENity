using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Manager.Vobs
{
    /// <summary>
    /// Handles merging data from StaticCache GameObjects in scene with Prefab data when loaded.
    ///
    /// Used during World loading only.
    /// </summary>
    public class VobCachePrefabManager : AbstractVobManager
    {
        private Dictionary<VirtualObjectType, Queue<GameObject>> _cachedGOs = new();

        /// <summary>
        /// When we load prefabs, we can't alter their GameObjects' parent-child relationship at the end (when we skipped at least 1 frame).
        /// </summary>
        private List<MergingTreeNode> _prefabsToMerge = new();

        private class MergingTreeNode
        {
            public string Name;
            public GameObject Go;
            public GameObject PrefabGo; // Will be used as new Go after Mesh* Components are copied over.
            public List<MergingTreeNode> Children = new();

            public MergingTreeNode(string name, GameObject go)
            {
                Name = name;
                Go = go;
            }
        }

        protected override void PreCreateVobs(List<IVirtualObject> rootVobs, GameObject rootGo)
        {
            PreFillCachedGoList();
        }

        protected override void PostCreateVobs()
        {
            foreach (var mergingPrefab in _prefabsToMerge)
            {
                MovePrefabElementsToExisting(mergingPrefab);
            }
        }

        /// <summary>
        /// We handle only VOBTypes which are also present in the cache.
        /// </summary>
        protected override bool SpawnObjectType(VirtualObjectType type)
        {
            return VobCacheManager.VobTypesToCache.Contains(type);
        }

        protected override void AddToMobInteractableList(IVirtualObject vob, GameObject go)
        {
            throw new NotImplementedException();
        }

        protected override GameObject CreateItem(Item vob, GameObject parent = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// As a result we get something like:
        ///   _cachedGOs = {
        ///     zCVob => [GameObject, GameObject, ...]
        ///     zCVobAnimate => [GameObject]
        ///      ...
        ///   }
        /// </summary>
        private void PreFillCachedGoList()
        {
            // We fill list of static cache GameObjects to apply prefab data onto it when looped.
            foreach (var rootGo in new[]{TeleportParentGo, NonTeleportParentGo})
            {
                foreach (var vobTypeGOs in rootGo.GetAllDirectChildren())
                {
                    Enum.TryParse(vobTypeGOs.name, out VirtualObjectType type); // Name of GameObject == a VobType enum label
                    _cachedGOs.Add(type, new Queue<GameObject>()); // Init

                    foreach (var cachedGo in vobTypeGOs.GetAllDirectChildren())
                    {
                        _cachedGOs[type].Enqueue(cachedGo);
                    }
                }
            }
        }


        protected override GameObject CreateDefaultMesh(IVirtualObject vob, GameObject parent = null, bool nonTeleport = false)
        {
            // The VOB is a new one. We create its mesh normally.
            if (!VobCacheManager.VobTypesToCache.Contains(vob.Type))
            {
                return base.CreateDefaultMesh(vob, parent, nonTeleport);
            }

            // Now load the GameObject from cached List and apply Prefab components to it
            var cachedObject = _cachedGOs[vob.Type].Dequeue();
            var prefab = GetPrefab(vob);

            MergePrefabWithExistingGo(prefab, cachedObject);

            return cachedObject;
        }

        /// <summary>
        /// Apply prefab elements onto the existing object.
        ///
        /// It's easier this way, as Unity has no option to Move a Component to another GameObject.
        /// We would therefore need to know every! Component type and how to copy data.
        /// In this case it's easier to simply copy MeshFilter+MeshRenderer (the only two elements inside Cached GOs) over.
        ///
        /// Stages are:
        /// 1. Prefab might contain GameObjects in regex style (e.g. BIP01*_SMALL). Find matching GO from existing GO and apply name.
        /// 2. Now create all MeshRenderer + MeshFilter on prefab objects mapping existing GOs component hierarchy.
        ///   2.1 Apply existing Meshes and Materials to Prefab objects
        ///   2.2 Exchange previously existing GO with the one from prefab (replace parent assignment only; new prefab GO contains all the Components now)
        ///
        /// Example existing hierarchy:
        /// BIP01
        ///   |- BIP01 CHESTLOCK
        ///   |- BIP01 CHEST_SMALL_1
        ///
        /// Example GOs (from a Prefab):
        /// BIP01
        ///   |- BIP01 CHEST_.*_1
        ///
        /// Merged GOs:
        /// BIP01
        ///   |- BIP01 CHESTLOCK     - from nodes; existing and nothing changed.
        ///   |- BIP01 CHEST_SMALL_1 - merged from Prefab and existing object. If Mesh* existed, they are copied over.
        /// </summary>
        private void MergePrefabWithExistingGo(GameObject prefab, GameObject existingGo)
        {
            var existingTree = new MergingTreeNode("Root", existingGo);
            var prefabTree = new MergingTreeNode("Root", prefab);
            existingTree.PrefabGo = prefab; // Some GOs have their Mesh* Components directly attached to root. We ensure they will get merged as well.

            var mergedTree = MergeTrees(existingTree, prefabTree);

            _prefabsToMerge.Add(mergedTree);
        }

        /// <summary>
        /// We create a containerized tree of a merged GameObject tree
        /// tree1 -> Primary
        /// tree2 -> Secondary (everything from this one will be replaced by tree1 if duplicate)
        /// </summary>
        private MergingTreeNode MergeTrees(MergingTreeNode existingMergingTree, MergingTreeNode prefabMergingTree)
        {
            if (prefabMergingTree == null)
                return existingMergingTree;
            if (existingMergingTree == null)
                return prefabMergingTree;

            MergingTreeNode mergedNode = existingMergingTree;

            // Create a dictionary to hold all children, using their values as keys
            Dictionary<string, MergingTreeNode> mergedChildren = new Dictionary<string, MergingTreeNode>();

            // Add children from existingTree
            foreach (var child in existingMergingTree.Go.GetAllDirectChildren())
            {
                mergedChildren[child.name] = new MergingTreeNode(child.name, child);
            }

            // Merge or add children from prefabTree
            foreach (var child in prefabMergingTree.Go.GetAllDirectChildren())
            {
                if (mergedChildren.TryGetValue(child.name, out MergingTreeNode existingChild))
                {
                    existingChild.PrefabGo = child;
                    MoveMeshComponentsFromPrefab(child, existingChild.Go);
                    // If the child already exists, merge it
                    mergedChildren[child.name] = MergeTrees(existingChild, new MergingTreeNode(child.name, child));
                }
                else
                {
                    // If it doesn't exist, add it
                    mergedChildren[child.name] = new MergingTreeNode(child.name, child);
                }
            }

            mergedNode.Children = mergedChildren.Values.ToList();

            return mergedNode;
        }

        private void MoveMeshComponentsFromPrefab(GameObject prefab, GameObject existing)
        {
            // Copy MeshFilter+MeshRenderer data from existing to prefab GO
            if (prefab.TryGetComponent<MeshFilter>(out var prefabMeshFilter))
            {
                var existingMeshFilter = existing.GetComponent<MeshFilter>();
                prefabMeshFilter.sharedMesh = existingMeshFilter.sharedMesh;
            }

            if (prefab.TryGetComponent<Renderer>(out var prefabRenderer))
            {
                var existingRenderer = existing.GetComponent<Renderer>();
                prefabRenderer.sharedMaterials = existingRenderer.sharedMaterials;

                // FIXME - Move also SkinnedMeshRenderer Bones and BoneWeights
            }

            // First we move existing GO below parent of Prefab GO
            existing.SetParent(prefab.transform.parent.gameObject);
        }

        /// <summary>
        ///
        /// </summary>
        private void MovePrefabElementsToExisting(MergingTreeNode node)
        {
            foreach (var child in node.Children)
            {
                // It means a GameObject was in both trees. We now leverage the prefab one as the new one by:
                // 1. changing parent relationship of existing and prefab GO
                // 2. Copying data from MeshFilter+MeshRenderer from existing to prefab GO
                if (child.PrefabGo != null)
                {

                    // Now we move Prefab GO at spot where Child GO was previously
                    child.PrefabGo.SetParent(node.Go);
                }
                else
                {
                    child.Go.SetParent(node.Go);
                }

                MovePrefabElementsToExisting(child);
            }
        }
    }
}
