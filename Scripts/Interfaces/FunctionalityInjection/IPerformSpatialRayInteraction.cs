#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    ///
    /// </summary>
    public interface IPerformSpatialRayInteraction
    {
        Transform spatialProxyRayDriverTransform { get; set; }
    }

    public static class IPerformSpatialRayInteractionMethods
    {
        internal delegate void UpdateSpatialRayDelegate(IPerformSpatialRayInteraction caller);

        internal static UpdateSpatialRayDelegate updateSpatialRay { private get; set; }

        /// <summary>
        ///
        /// </summary>
        public static void UpdateSpatialRay(this IPerformSpatialRayInteraction obj)
        {
            updateSpatialRay(obj);
        }
    }
}
#endif
