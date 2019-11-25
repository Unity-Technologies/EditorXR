using UnityEngine;

namespace Unity.Labs.EditorXR.Extensions
{
    static class Vector3Extensions
    {
        const float k_OneThird = 1f / 3;

        /// <summary>
        /// Returns a vector where each component is inverted (1/x)
        /// </summary>
        /// <returns>The inverted vector</returns>
        public static Vector3 Inverse(this Vector3 vec)
        {
            return new Vector3(1 / vec.x, 1 / vec.y, 1 / vec.z);
        }

        /// <summary>
        /// Returns the minimum of all vector components
        /// </summary>
        /// <returns>The minimum value</returns>
        public static float MinComponent(this Vector3 vec)
        {
            return Mathf.Min(Mathf.Min(vec.x, vec.y), vec.z);
        }

        /// <summary>
        /// Returns the maximum of all vector components
        /// </summary>
        /// <returns>The maximum value</returns>
        public static float MaxComponent(this Vector3 vec)
        {
            return Mathf.Max(Mathf.Max(vec.x, vec.y), vec.z);
        }

        /// <summary>
        /// Returns a vector where each component is the absolute value of the original (abs(x))
        /// </summary>
        /// <returns>The absolute value vector</returns>
        public static Vector3 Abs(this Vector3 vec)
        {
            vec.x = Mathf.Abs(vec.x);
            vec.y = Mathf.Abs(vec.y);
            vec.z = Mathf.Abs(vec.z);
            return vec;
        }

        /// <summary>
        /// Returns the average of all vector components
        /// </summary>
        /// <returns>The average value</returns>
        public static float AveragedComponents(this Vector3 vec)
        {
            return (vec.x + vec.y + vec.z) * k_OneThird;
        }
    }
}
