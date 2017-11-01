#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Extensions
{
    static class Vector2Extensions
    {
        /// <summary>
        /// Returns a vector where each component is inverted (1/x)
        /// </summary>
        /// <returns>The inverted vector</returns>
        public static Vector2 Inverse(this Vector2 vec)
        {
            return new Vector2(1 / vec.x, 1 / vec.y);
        }

        /// <summary>
        /// Returns the minimum of all vector components
        /// </summary>
        /// <returns>The minimum value</returns>
        public static float MinComponent(this Vector2 vec)
        {
            return Mathf.Min(vec.x, vec.y);
        }

        /// <summary>
        /// Returns the maximum of all vector components
        /// </summary>
        /// <returns>The maximum value</returns>
        public static float MaxComponent(this Vector2 vec)
        {
            return Mathf.Max(vec.x, vec.y);
        }

        /// <summary>
        /// Returns a vector where each component is the absolute value of the original (abs(x))
        /// </summary>
        /// <returns>The absolute value vector</returns>
        public static Vector2 Abs(this Vector2 vec)
        {
            vec.x = Mathf.Abs(vec.x);
            vec.y = Mathf.Abs(vec.y);
            return vec;
        }

        /// <summary>
        /// Returns the average of all vector components
        /// </summary>
        /// <returns>The average value</returns>
        public static float AveragedComponents(this Vector2 vec)
        {
            return (vec.x + vec.y) * 0.5f;
        }
    }
}
#endif
