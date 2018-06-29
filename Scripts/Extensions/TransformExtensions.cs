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
            if ((forward.y * forward.y) >= 0.5f)
            {
                forward = -target.up * Mathf.Sign(forward.y);
            }
            else
            {
                if (target.up.y < 0.0f)
                {
                    forward = -forward;
                }
            }
            forward.y = 0.0f;
            return forward.normalized;
        }
    }
}
#endif
