using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Implementors can set the color of the default ray
    /// </summary>
    public interface ISetDefaultRayColor
    {
    }

    public static class ISetDefaultRayColorMethods
    {
        internal static Action<Transform, Color> setDefaultRayColor { get; set; }

        /// <summary>
        /// Set the color of the default ray
        /// </summary>
        /// <param name="rayOrigin">The ray on which to set the color</param>
        /// <param name="color">The color to set on the default ray</param>
        public static void SetDefaultRayColor(this ISetDefaultRayColor obj, Transform rayOrigin, Color color)
        {
            setDefaultRayColor(rayOrigin, color);
        }
    }
}
