using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Handles
{
    /// <summary>
    /// Event data for BaseHandle.DragEventCallback
    /// </summary>
    class HandleEventData
    {
        /// <summary>
        /// The source transform from where the ray is cast
        /// </summary>
        public Transform rayOrigin;

        /// <summary>
        /// Whether this pointer was within range to be considered "direct"
        /// </summary>
        public bool direct;

        /// <summary>
        /// Change in position between last frame and this frame
        /// </summary>
        public Vector3 deltaPosition;

        /// <summary>
        /// Change in rotation between last frame and this frame
        /// </summary>
        public Quaternion deltaRotation;

        public HandleEventData(Transform rayOrigin, bool direct)
        {
            this.rayOrigin = rayOrigin;
            this.direct = direct;
            this.deltaPosition = Vector3.zero;
            this.deltaRotation = Quaternion.identity;
        }
    }
}
