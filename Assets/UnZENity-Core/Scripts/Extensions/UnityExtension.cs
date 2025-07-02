using UnityEngine;

namespace GUZ.Core.Extensions
{
    public static class UnityExtension
    {
        /// <summary>
        /// Transform Vector3 to Unity Vector3.
        /// transformScale - Gothic positions are in cm, but Unity in m. (factor 100). Most of the time we just transform it directly.
        /// </summary>
        public static System.Numerics.Vector3 ToZkVector(this Vector3 vector3, bool transformScale = true)
        {
            var vector = new System.Numerics.Vector3
            {
                X = vector3.x,
                Y = vector3.y,
                Z = vector3.z
            };

            if (transformScale)
                return vector * 100;
            else
                return vector;
        }
    }
}
