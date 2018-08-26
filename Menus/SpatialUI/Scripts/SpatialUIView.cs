#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    /// <summary>
    /// Mandates that derived classes implement core SpatialUI implementation
    /// The SpatialMenu is the first robust implementation, SpatialContextUI is planned to derive from core
    /// </summary>
    public abstract class SpatialUIView : MonoBehaviour, IControlHaptics, IControlInputIntersection,
        INodeToRay
    {
        public enum SpatialInterfaceInputMode
        {
            Translation,
            Rotation,
            GhostRay,
            ExternalInputRay
        }

        [Header("Haptic Pulses")]
        [SerializeField]
        protected HapticPulse m_AdaptivePositionPulse;

        [SerializeField]
        protected HapticPulse m_HighlightUIElementPulse;

        [SerializeField]
        protected HapticPulse m_SustainedHoverUIElementPulse;

        protected SpatialUIToggle m_SpatialPinToggle { get; set; }

        public HapticPulse highlightUIElementPulse { get { return m_HighlightUIElementPulse; } }
    }
}
#endif
