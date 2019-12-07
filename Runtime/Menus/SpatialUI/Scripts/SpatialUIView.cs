using Unity.Labs.EditorXR.Core;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Menus
{
    /// <summary>
    /// Mandates that derived classes implement core SpatialUI implementation
    /// The SpatialMenu is the first robust implementation, SpatialContextUI is planned to derive from core
    /// </summary>
    abstract class SpatialUIView : MonoBehaviour, IUsesControlHaptics, INodeToRay
    {
        /// <summary>
        /// Enum that defines the allowed SpatialUI input-modes
        /// </summary>
        public enum SpatialInterfaceInputMode
        {
            Neutral,
            Ray,
            TriggerAffordanceRotation
        }

        [Header("Haptic Pulses")]
        [SerializeField]
        protected HapticPulse m_HighlightUIElementPulse;

        [SerializeField]
        protected HapticPulse m_SustainedHoverUIElementPulse;

#if !FI_AUTOFILL
        IProvidesControlHaptics IFunctionalitySubscriber<IProvidesControlHaptics>.provider { get; set; }
#endif
    }
}
