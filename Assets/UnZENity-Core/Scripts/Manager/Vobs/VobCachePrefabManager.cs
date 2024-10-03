using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Properties;
using MyBox;
using UnityEngine;
using ZenKit.Vobs;
using Object = UnityEngine.Object;

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
        /// When we load prefabs, we can't alter their GameObjects' parent-child relationship immediately (earliest when we skipped at least 1 frame).
        /// We therefore cache these elements and merge them ad PostCreateVobs() stage.
        /// </summary>
        private List<MergingTreeNode> _prefabsToMerge = new();

        /// <summary>
        /// Temp GameObject where we store StaticCache GO replacements before removing them fully at the end of Prefab->CacheGO transition.
        /// </summary>
        private GameObject _graveyard;

        private class MergingTreeNode
        {
            public GameObject Go;
            public GameObject PrefabGo; // Will be used as new Go after Mesh* Components are copied over.
            public List<MergingTreeNode> Children = new();

            public MergingTreeNode(GameObject go)
            {
                Go = go;
            }
        }

        protected override void PreCreateVobs(List<IVirtualObject> rootVobs, GameObject rootGo)
        {
            // The elements are already created by static cache.
            TeleportParentGo = rootGo.FindChildRecursively("Teleport").gameObject;
            NonTeleportParentGo = rootGo.FindChildRecursively("NonTeleport").gameObject;

            CreateParentVobStructure();

            PreFillCachedGoList();

            // We will remove everything below this GameObject once Prefabs are merged with StaticVob GOs.
            _graveyard = new GameObject("Graveyard");
        }

        protected override void PostCreateVobs()
        {
            foreach (var mergingPrefab in _prefabsToMerge)
            {
                PostBuildVobTree(mergingPrefab, mergingPrefab.Go.GetParent());

                AddToMobInteractableList(mergingPrefab.PrefabGo.GetComponent<VobProperties>().Properties.Type, mergingPrefab.Go);
            }

            Object.Destroy(_graveyard);
        }

        /// <summary>
        /// We handle only VOBTypes which are also present in the cache.
        /// </summary>
        protected override bool SpawnObjectType(VirtualObjectType type)
        {
            return VobCacheManager.VobTypesToCache.Contains(type);
        }

        protected override GameObject CreateItem(Item vob, GameObject parent = null)
        {
            throw new NotImplementedException();
        }

        private void CreateParentVobStructure()
        {
            foreach (var child in TeleportParentGo.GetAllDirectChildren())
            {
                Enum.TryParse(child.name, out VirtualObjectType type);

                ParentGosTeleport.Add(type, child);
            }

            foreach (var child in NonTeleportParentGo.GetAllDirectChildren())
            {
                Enum.TryParse(child.name, out VirtualObjectType type);

                ParentGosNonTeleport.Add(type, child);
            }
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

        protected override void AddToMobInteractableList(VirtualObjectType type, GameObject go)
        {
            // When this method is called during Prefab loading, we don't have the proper VobProperties applied. Therefore skip this to a later stage.
            // FIXME - We need to fill a temp list with all objects and apply it in PostCreateVobs() via base.AddMobInteractableList() at the end.
        }

        protected override GameObject CreateDefaultMesh(IVirtualObject vob, GameObject parent = null, bool nonTeleport = false)
        {
            // We need to check if there is a VOB which isn't inside the StaticCache GOs.
            // e.g. Harp has no meshes but is still inside VOBs. We need to skip it as it has no StaticCache GO representation in scene.
            var vobName = vob.ShowVisual ? vob.Visual.Name : vob.Name;
            if (_cachedGOs[vob.Type].Peek().name != vobName)
            {
                return null;
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
        /// We would therefore need to know every! Component type and how to copy data from Prefab to existing GO.
        /// In this case it's easier to simply copy MeshFilter+MeshRenderer (the only two elements inside Cached GOs) over.
        ///
        /// Options for merging are:
        /// 1. Existing StaticCache GO has no corresponding Prefab entry - no change
        /// 2. Existing StaticCache GO has a Prefab entry to overwrite - switch the existing GO with Prefab GO at the end
        /// 3. No existing StaticCache GO but a Prefab entry - simply add prefab entry to GO
        ///
        /// When we have an existing StaticCache GO and Prefab entry, then we will copy *Renderer and MeshFilter values over to Prefab before making it new master
        /// 2. Now create all MeshRenderer + MeshFilter on prefab objects mapping existing GOs component hierarchy.
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
        ///   |- BIP01 CHESTLOCK     - from existing GO; nothing changed.
        ///   |- BIP01 CHEST_SMALL_1 - merged from Prefab and existing object. If Mesh* existed, they are copied over.
        /// </summary>
        private void MergePrefabWithExistingGo(GameObject prefab, GameObject existingGo)
        {
            var existingTree = new MergingTreeNode(existingGo);
            var prefabTree = new MergingTreeNode(prefab);
            existingTree.PrefabGo = prefab; // Some GOs have their Mesh* Components directly attached to root. We ensure they will get merged as well.

            var mergedTree = MergeTrees(existingTree, prefabTree);

            _prefabsToMerge.Add(mergedTree);
        }

        /// <summary>
        /// We create a containerized tree of a merged GameObject tree without need to set parent-child relationship of GameObjects now.
        /// (It's easier to walk the tree now and apply mergings at the end)
        /// </summary>
        private MergingTreeNode MergeTrees(MergingTreeNode existingMergingTree, MergingTreeNode prefabMergingTree)
        {
            if (prefabMergingTree == null)
            {
                return existingMergingTree;
            }
            else if (existingMergingTree == null)
            {
                return prefabMergingTree;
            }

            var mergedNode = existingMergingTree;

            // Create a dictionary to hold all children, using their values as keys
            var mergedChildren = new Dictionary<string, MergingTreeNode>();

            // Add children from existingTree
            foreach (var child in existingMergingTree.Go.GetAllDirectChildren())
            {
                mergedChildren[child.name] = new MergingTreeNode(child);
            }

            // Merge or add children from prefabTree
            foreach (var child in prefabMergingTree.Go.GetAllDirectChildren())
            {
                // If the child already exists, merge it
                if (mergedChildren.TryGetValue(child.name, out MergingTreeNode existingChild))
                {
                    existingChild.PrefabGo = child; // This information will tell us later to merge both GOs together.
                    mergedChildren[child.name] = MergeTrees(existingChild, new MergingTreeNode(child));
                }
                // If it doesn't exist simply add it
                else
                {
                    mergedChildren[child.name] = new MergingTreeNode(child);
                }
            }

            mergedNode.Children = mergedChildren.Values.ToList();

            return mergedNode;
        }

        /// <summary>
        /// We build new parent-child relationship of GameObject
        /// </summary>
        private void PostBuildVobTree(MergingTreeNode node, GameObject parent)
        {
            // It means a GameObject was in both trees. We now leverage the prefab one as the new one by:
            // 1. changing parent relationship of existing and prefab GO
            // 2. Copying data from MeshFilter+MeshRenderer from existing to prefab GO
            if (node.PrefabGo != null)
            {
                PostMoveMeshComponentsFromPrefab(node.PrefabGo, node.Go);

                node.PrefabGo.transform.SetLocalPositionAndRotation(node.Go.transform.localPosition, node.Go.transform.localRotation);

                // We move Prefab GO at spot where existing go was previously located
                node.PrefabGo.SetParent(parent);

                // As we don't need the existing GO any longer, we move it to the Graveyard.
                node.Go.SetParent(_graveyard);
            }
            else
            {
                node.Go.SetParent(parent);
            }

            foreach (var child in node.Children)
            {
                PostBuildVobTree(child, node.PrefabGo ?? node.Go);
            }
        }

        /// <summary>
        /// We move Mesh Components from existing GO to prefab GO.
        ///
        /// Background: During time of StaticCache apply, we create only MeshFilter and *Renderer data.
        /// Now we copy over this data to Prefab GOs as the prefab ones can have many more components and it's easier this way around.
        /// </summary>
        private void PostMoveMeshComponentsFromPrefab(GameObject prefab, GameObject existing)
        {
            if (existing.TryGetComponent<MeshFilter>(out var existingMeshFilter))
            {
                var prefabMeshFilter = prefab.GetOrAddComponent<MeshFilter>();
                prefabMeshFilter.sharedMesh = existingMeshFilter.sharedMesh;
            }

            if (existing.TryGetComponent<Renderer>(out var existingRenderer))
            {
                Renderer prefabRenderer = null;
                if (existingRenderer is MeshRenderer)
                {
                    prefabRenderer = prefab.GetOrAddComponent<MeshRenderer>();
                }
                else if (existingRenderer is SkinnedMeshRenderer)
                {
                    // FIXME - Move also SkinnedMeshRenderer Bones and BoneWeights
                }
                prefabRenderer.sharedMaterials = existingRenderer.sharedMaterials;

            }
        }
    }
}
