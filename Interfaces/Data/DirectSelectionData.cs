using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Stores the state of a direct selection
    /// </summary>
    public struct DirectSelectionData
    {
        /// <summary>
        /// The object which is selected
        /// </summary>
        public GameObject gameObject { get; set; }

        /// <summary>
        /// The point in world space where the tester intersects with the object
        /// </summary>
        public Vector3 contactPoint { get; set; }
    }
}
