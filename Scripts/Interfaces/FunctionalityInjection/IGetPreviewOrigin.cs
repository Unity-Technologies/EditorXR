using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Implementors receive a preview origin transform
    /// </summary>
    public interface IGetPreviewOrigin
    {
    }

    public static class IGetPreviewOriginMethods
    {
        internal static Func<Transform, Transform> getPreviewOriginForRayOrigin { get; set; }

        /// <summary>
        /// Get the preview transform attached to the given rayOrigin
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin where the preview will occur</param>
        public static Transform GetPreviewOriginForRayOrigin(this IGetPreviewOrigin obj, Transform rayOrigin)
        {
            return getPreviewOriginForRayOrigin(rayOrigin);
        }
    }
}
