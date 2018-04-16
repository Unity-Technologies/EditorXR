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
        internal delegate void UpdateSpatialProxyRayDelegate(ISpatialProxyRay caller);

        internal static UpdateSpatialProxyRayDelegate updateSpatialProxyRay { private get; set; }

        /// <summary>
        ///
        /// </summary>
        public static void UpdateSpatialProxyRay(this ISpatialProxyRay obj)
        {
            updateSpatialProxyRay(obj);
        }
    }
}
#endif
