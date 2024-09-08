using System.Collections.Generic;
using UnityEngine;

namespace GUZ.Core.Caches
{
    public class MeshCache : MonoBehaviour
    {
        public static Dictionary<string, Mesh> Meshes = new();
    }
}
