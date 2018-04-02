#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// The detectable types of spatial input that a node can perform
    /// </summary>
    [Flags]
    public enum SpatialInputType
    {
        None = 0, // 0
        X = 1 << 0, // 1 / Allow for flag-based polling of which axis' are involved in either a drag or rotation.
        Y = 1 << 1, // 2 / Euler's used in order to allow for polling either for translation or rotation
        Z = 1 << 2, // 4
        DragTranslation = 1 << 3, // 8
        SingleAxisRotation = (X ^ Y ^ Z), // Validate that only one axis is being rotated
        FreeRotation = (X & Y) | (Z & Y) | (Z & X), // Detect at least two axis' crossing their local rotation threshold
    }

    public sealed class SpatialInputDetectionModule : MonoBehaviour
    {
        [SerializeField]
        HapticPulse m_TranslationPulse; // The pulse performed on a node performing a spatial scroll while only in translation mode
        
        [SerializeField]
        HapticPulse m_FreeRotationPulse; // The pulse performed on a node performing a spatial scroll while only in free-rotation mode
        
        [SerializeField]
        HapticPulse m_SingleAxistRotationPulse; // The pulse performed on a node performing a spatial scroll while only in single-axis rotation mode

        // Collection housing objects whose spatial input is being processed
        readonly Dictionary<Node, SpatialInputType> m_ActiveSpatialNodes = new Dictionary<Node, SpatialInputType>();

        // Perform a constant haptic for translation/dragging
        // Perform a sharply pulsing haptic for rotation
        // Perform a gradual pulsing for free rotation
        // Monitor and perform the relevant pulses for all registered nodes, when spatial scrolling is being performed by that node

        void Update()
        {
            // Iterate over all ACTIVE(performing spatial input) nodes perform a spatial scroll
            // Update the SpatialInputType for each ACTIVE node
            // Set SpatialInputType for nodes not performing any spatial input to NONE
            // Otherwise, set relevant SpatialInputType value
        }

        public SpatialInputType GetSpatialInputTypeForNode(IDetectSpatialInputType obj, Node node)
        {
            // Iterate on the node to active state collection
            // Return none for those not performing a spatial input action
            // Return the relevant SpatialInputType for a given node otherwise

            SpatialInputType spatialInputType = SpatialInputType.None;
            foreach (var nodeToInputType in m_ActiveSpatialNodes)
            {
                if (nodeToInputType.Key == node)
                {
                    spatialInputType = nodeToInputType.Value;
                    break;
                }
            }
            
            return spatialInputType;
        }
    }
}
#endif
