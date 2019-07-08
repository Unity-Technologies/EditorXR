using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    delegate void ForEachRayOriginCallback(Transform rayOrigin);

    interface IForEachRayOrigin
    {
    }

    static class IForEachRayOriginMethods
    {
        internal static Action<ForEachRayOriginCallback> forEachRayOrigin { get; set; }

        public static void ForEachRayOrigin(this IForEachRayOrigin obj, ForEachRayOriginCallback callback)
        {
            forEachRayOrigin(callback);
        }
    }
}
