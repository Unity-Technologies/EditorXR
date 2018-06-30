#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Extensions
{
    static class TransformExtensions
    {
        public static Bounds TransformBounds(this Transform transform, Bounds localBounds)
        {
            var center = transform.TransformPoint(localBounds.center);

            // transform the local extents' axes
            var extents = localBounds.extents;
            var axisX = transform.TransformVector(extents.x, 0, 0);
            var axisY = transform.TransformVector(0, extents.y, 0);
            var axisZ = transform.TransformVector(0, 0, extents.z);

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        public static Vector3 XZForward(this Transform target)
        {
            var forward = target.forward;
            var up = forward.y;
            const float kMid = 0.5f;
            const float kMin = 0.0f;
            if ((up * up) >= kMid)
            {
                forward = -target.up * Mathf.Sign(up);
            }
            else if (target.up.y < kMin)
            {
                forward = -forward;
            }

            forward.y = 0.0f;
            return forward.normalized;
        }
    }

}
#endif