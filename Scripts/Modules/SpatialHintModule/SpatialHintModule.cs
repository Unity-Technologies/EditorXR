using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public sealed class SpatialHintModule : MonoBehaviour, ISystemModule, IConnectInterfaces, IInstantiateUI,
        INodeToRay, IRayVisibilitySettings
    {
        public enum SpatialHintStateFlags
        {
            Hidden,
            PreDragReveal,
            Scrolling,
            CenteredScrolling,
        }

        [SerializeField]
        SpatialHintUI m_SpatialHintUI;

        SpatialHintStateFlags m_State;
        Node m_ControllingNode;

        public SpatialHintStateFlags state
        {
            get { return m_State; }
            set
            {
                m_State = value;
                switch (m_State)
                {
                    case SpatialHintStateFlags.Hidden:
                        m_SpatialHintUI.centeredScrolling = false;
                        m_SpatialHintUI.preScrollArrowsVisible = false;
                        m_SpatialHintUI.secondaryArrowsVisible = false;
                        this.RemoveRayVisibilitySettings(this.RequestRayOriginFromNode(m_ControllingNode), this);
                        controllingNode = Node.None;
                        break;
                    case SpatialHintStateFlags.PreDragReveal:
                        m_SpatialHintUI.centeredScrolling = false;
                        m_SpatialHintUI.preScrollArrowsVisible = true;
                        m_SpatialHintUI.secondaryArrowsVisible = true;
                        break;
                    case SpatialHintStateFlags.Scrolling:
                        m_SpatialHintUI.centeredScrolling = false;
                        m_SpatialHintUI.preScrollArrowsVisible = false;
                        m_SpatialHintUI.scrollVisualsVisible = true;
                        break;
                    case SpatialHintStateFlags.CenteredScrolling:
                        m_SpatialHintUI.centeredScrolling = true;
                        m_SpatialHintUI.preScrollArrowsVisible = false;
                        m_SpatialHintUI.scrollVisualsVisible = true;
                        break;
                }
            }
        }

        Node controllingNode
        {
            set
            {
                var controllingNode = m_SpatialHintUI.controllingNode;
                if (value == controllingNode)
                    return;

                m_ControllingNode = value;
                if (m_ControllingNode != Node.None)
                    state = SpatialHintStateFlags.PreDragReveal;

                m_SpatialHintUI.controllingNode = value;
            }
        }

        Vector3 spatialHintScrollVisualsRotation { set { m_SpatialHintUI.scrollVisualsRotation = value; } }

        Transform spatialHintContentContainer { get { return m_SpatialHintUI.contentContainer; } }

        void OnEnable()
        {
            m_SpatialHintUI = this.InstantiateUI(m_SpatialHintUI.gameObject).GetComponent<SpatialHintUI>();
            this.ConnectInterfaces(m_SpatialHintUI);
        }

        internal void PulseScrollArrows()
        {
            m_SpatialHintUI.PulseScrollArrows();
        }

        internal void SetState(SpatialHintStateFlags newState)
        {
            state = newState;
        }

        internal void SetPosition(Vector3 newPosition)
        {
            spatialHintContentContainer.position = newPosition;
        }

        internal void SetContainerRotation(Quaternion newRotation)
        {
            m_SpatialHintUI.transform.rotation = newRotation;
        }

        internal void SetShowHideRotationTarget(Vector3 target)
        {
            spatialHintScrollVisualsRotation = target;
        }

        internal void LookAt(Vector3 position)
        {
            var orig = spatialHintContentContainer.rotation;
            spatialHintContentContainer.LookAt(position);
            spatialHintContentContainer.rotation = orig;
        }

        internal void SetDragThresholdTriggerPosition(Vector3 position)
        {
            if (state == SpatialHintStateFlags.Hidden || position == m_SpatialHintUI.scrollVisualsDragThresholdTriggerPosition)
                return;

            m_SpatialHintUI.scrollVisualsDragThresholdTriggerPosition = position;
        }

        internal void SetSpatialHintControlNode(Node controlNode)
        {
            controllingNode = controlNode;
            this.AddRayVisibilitySettings(this.RequestRayOriginFromNode(m_ControllingNode), this, false, false);
        }
    }
}
