using UnityEngine;

namespace Unity.EditorXR.Handles
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
        /// The camera from where the ray is cast if this event came from the screen
        /// </summary>
        public Camera camera { private get; set; }

        /// <summary>
        /// Whether this pointer was within range to be considered "direct"
        /// </summary>
        public bool direct;

        /// <summary>
        /// The screen position of the touch/mouse event if it came from the screen
        /// </summary>
        public Vector3 position { private get; set; }

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

        public Ray GetRay()
        {
            return camera == null ?
                new Ray(rayOrigin.position, rayOrigin.forward) :
                camera.ScreenPointToRay(position);
        }
    }
}
