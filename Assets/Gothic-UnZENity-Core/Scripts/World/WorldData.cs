using System.Collections.Generic;
using GUZ.Core.Caches;
using UnityEngine;
using ZenKit;
using ZenKit.Vobs;

namespace GUZ.Core.World
{
    /// <summary>
    /// Parsed ZenKit World data is arranged in a way to easily be usable by Unity objects.
    /// E.g. by providing sub meshes.
    /// </summary>
    public class WorldData
    {
        // We need to store it as we need the pointer to it for load+save of un-cached vobs.
        public List<IVirtualObject> Vobs;

        // Cached objects - For performance reasons we only allow them cached. Otherwise every loop and getter will load them again.
        public CachedMesh Mesh;
        public CachedBspTree BspTree;
        public CachedWayNet WayNet;

        public List<SubMeshData> SubMeshes;


        public class SubMeshData
        {
            public IMaterial Material;
            public TextureCache.TextureArrayTypes TextureArrayType;

            public readonly List<Vector3> Vertices = new();
            public readonly List<int> Triangles = new();
            public readonly List<Vector4> Uvs = new();
            public readonly List<Vector3> Normals = new();
            public readonly List<Color32> BakedLightColors = new();
            public readonly List<Vector2> TextureAnimations = new();
        }
    }
}
