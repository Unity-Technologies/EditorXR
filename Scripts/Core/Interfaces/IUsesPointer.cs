#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    interface IUsesPointer
    {
    }

    static class IUsesPointerMethods
    {
        internal static Func<Transform, float> getPointerLength { get; set; }

        public static float GetPointerLength(this IUsesPointer obj, Transform rayOrigin)
        {
            return getPointerLength(rayOrigin);
        }

        public static Vector3 GetPointerPosition(this IUsesPointer obj, Transform rayOrigin)
        {
            return rayOrigin.position + rayOrigin.forward * obj.GetPointerLength(rayOrigin);
        }
    }
}
#endif
