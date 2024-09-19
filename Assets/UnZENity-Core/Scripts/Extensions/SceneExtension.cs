using System.Linq;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Extensions
{
    public static class SceneExtension
    {
        [CanBeNull]
        public static T GetComponentInChildren<T>(this Scene scene)
        {
            return scene.GetRootGameObjects().Select(i => i.GetComponentInChildren<T>()).FirstOrDefault(i => i != null);
        }
    }
}
