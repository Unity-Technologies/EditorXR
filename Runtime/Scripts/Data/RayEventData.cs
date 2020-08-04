using Unity.EditorXR.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    sealed class RayEventData : PointerEventData
    {
        /// <summary>
        /// The root from where the ray is cast
        /// </summary>
        public Transform rayOrigin { get; set; }

        /// <summary>
        /// The camera from where the ray is cast if this event came from the screen
        /// </summary>
        public Camera camera { get; set; }

        /// <summary>
        /// The node associated with the ray
        /// </summary>
        public Node node { get; set; }

        /// <summary>
        /// The length of the direct selection pointer
        /// </summary>
        public float pointerLength { get; set; }

        public RayEventData(EventSystem eventSystem) : base(eventSystem) {}
    }
}
