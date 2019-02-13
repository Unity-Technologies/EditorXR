using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Implementors can get the color of the default ray
    /// </summary>
    public interface IGetDefaultRayColor
    {
    }

    public static class IGetDefaultRayColorMethods
    {
        internal static Func<Transform, Color> getDefaultRayColor { get; set; }

        /// <summary>
        /// Get the color of the default ray
        /// <param name="rayOrigin">The ray on which to set the color</param>
        /// </summary>
        public static Color GetDefaultRayColor(this IGetDefaultRayColor obj, Transform rayOrigin)
        {
            return getDefaultRayColor(rayOrigin);
        }
    }
}
