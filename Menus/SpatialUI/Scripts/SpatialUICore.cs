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
    public abstract class SpatialUICore : MonoBehaviour, IControlHaptics, IControlInputIntersection, IRayVisibilitySettings,
        INodeToRay
    {
        public enum SpatialInterfaceInputMode
        {
            Translation,
            Rotation,
            GhostRay,
            ExternalInputRay,
            BCI
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

        protected List<Node> controllingNodes { get; set; }


        public void addControllingNode(Node node)
        {
            if (controllingNodes.Contains(node))
                return;

            controllingNodes.Add(node);

            // Set priority to 10, in order to suppress any standard ray visibility settings from overriding
            this.AddRayVisibilitySettings(this.RequestRayOriginFromNode(node), this, false, false, 10);
        }

        public void removeControllingNode(Node node)
        {
            if (!controllingNodes.Contains(node))
                return;

            controllingNodes.Remove(node);

            this.RemoveRayVisibilitySettings(this.RequestRayOriginFromNode(node), this);
        }
    }
}
#endif
