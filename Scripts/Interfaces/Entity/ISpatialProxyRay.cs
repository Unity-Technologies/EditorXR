#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    ///
    /// </summary>
    public interface ISpatialProxyRay
    {
        Transform spatialProxyRayOrigin { get; set; }
        DefaultProxyRay spatialProxyRay { get; set; }
    }

    public static class ISpatialProxyRayMethods
    {
        internal delegate Transform InitializeSpatialProxyRayDelegate(ISpatialProxyRay caller, Transform rayOrigin, GameObject spatialProxyRayPrefab);

        internal static InitializeSpatialProxyRayDelegate initializeSpatialProxyRay { private get; set; }

        /// <summary>
        ///
        /// </summary>
        public static Transform InitializeSpatialProxyRay(this ISpatialProxyRay obj, Transform rayOrigin, GameObject spatialProxyRayPrefab)
        {
            return initializeSpatialProxyRay(obj, rayOrigin, spatialProxyRayPrefab);
        }

        internal delegate void UpdateSpatialProxyRayLengthDelegate(ISpatialProxyRay caller);

        internal static UpdateSpatialProxyRayLengthDelegate updateSpatialUIRayLengthLength { private get; set; }

        /// <summary>
        ///
        /// </summary>
        public static void UpdateSpatialProxyRayLength(this ISpatialProxyRay obj)
        {
            updateSpatialUIRayLengthLength(obj);
        }
    }
}
#endif
