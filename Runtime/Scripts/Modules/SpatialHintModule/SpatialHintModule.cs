using Unity.EditorXR.Core;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Menus;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;

namespace Unity.EditorXR.Modules
{
    // TODO: Remove load order when switching to non-static FI
    [ModuleOrder(ModuleOrders.SpatialHintModuleLoadOrder)]
    sealed class SpatialHintModule : ScriptableSettings<SpatialHintModule>, IUsesConnectInterfaces, IInstantiateUI,
        INodeToRay, IUsesRayVisibilitySettings, IDelayedInitializationModule, IProvidesControlSpatialHinting
    {
#pragma warning disable 649
        [SerializeField]
        SpatialHintUI m_SpatialHintUIPrefab;
#pragma warning restore 649

        SpatialHintUI m_SpatialHintUI;

        SpatialHintState m_State;
        Node m_ControllingNode;

        public SpatialHintState state
        {
            get { return m_State; }
            set
            {
                m_State = value;
                switch (m_State)
                {
                    case SpatialHintState.Hidden:
                        m_SpatialHintUI.centeredScrolling = false;
                        m_SpatialHintUI.preScrollArrowsVisible = false;
                        m_SpatialHintUI.secondaryArrowsVisible = false;
                        this.RemoveRayVisibilitySettings(this.RequestRayOriginFromNode(m_ControllingNode), this);
                        controllingNode = Node.None;
                        break;
                    case SpatialHintState.PreDragReveal:
                        m_SpatialHintUI.centeredScrolling = false;
                        m_SpatialHintUI.preScrollArrowsVisible = true;
                        m_SpatialHintUI.secondaryArrowsVisible = true;
                        break;
                    case SpatialHintState.Scrolling:
                        m_SpatialHintUI.centeredScrolling = false;
                        m_SpatialHintUI.preScrollArrowsVisible = false;
                        m_SpatialHintUI.scrollVisualsVisible = true;
                        break;
                    case SpatialHintState.CenteredScrolling:
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
                    state = SpatialHintState.PreDragReveal;

                m_SpatialHintUI.controllingNode = value;
            }
        }

        Vector3 spatialHintScrollVisualsRotation
        {
            set { m_SpatialHintUI.scrollVisualsRotation = value; }
        }

        Transform spatialHintContentContainer
        {
            get { return m_SpatialHintUI.contentContainer; }
        }

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesRayVisibilitySettings IFunctionalitySubscriber<IProvidesRayVisibilitySettings>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        public void LoadModule() { }

        public void UnloadModule() { }

        public void Initialize()
        {
            m_SpatialHintUI = this.InstantiateUI(m_SpatialHintUIPrefab.gameObject).GetComponent<SpatialHintUI>();
            this.ConnectInterfaces(m_SpatialHintUI);
        }

        public void Shutdown()
        {
            if (m_SpatialHintUI)
                UnityObjectUtils.Destroy(m_SpatialHintUI.gameObject);
        }

        public void PulseSpatialHintScrollArrows()
        {
            m_SpatialHintUI.PulseScrollArrows();
        }

        public void SetSpatialHintState(SpatialHintState newState)
        {
            state = newState;
        }

        public void SetSpatialHintPosition(Vector3 newPosition)
        {
            spatialHintContentContainer.position = newPosition;
        }

        public void SetSpatialHintContainerRotation(Quaternion newRotation)
        {
            m_SpatialHintUI.transform.rotation = newRotation;
        }

        public void SetSpatialHintShowHideRotationTarget(Vector3 target)
        {
            spatialHintScrollVisualsRotation = target;
        }

        public void SetSpatialHintLookAtRotation(Vector3 position)
        {
            var orig = spatialHintContentContainer.rotation;
            spatialHintContentContainer.LookAt(position);
            spatialHintContentContainer.rotation = orig;
        }

        public void SetSpatialHintDragThresholdTriggerPosition(Vector3 position)
        {
            if (state == SpatialHintState.Hidden || position == m_SpatialHintUI.scrollVisualsDragThresholdTriggerPosition)
                return;

            m_SpatialHintUI.scrollVisualsDragThresholdTriggerPosition = position;
        }

        public void SetSpatialHintControlNode(Node controlNode)
        {
            controllingNode = controlNode;
            this.AddRayVisibilitySettings(this.RequestRayOriginFromNode(m_ControllingNode), this, false, false);
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var controlSpatialHintingSubscriber = obj as IFunctionalitySubscriber<IProvidesControlSpatialHinting>;
            if (controlSpatialHintingSubscriber != null)
                controlSpatialHintingSubscriber.provider = this;
#endif
        }
        public void UnloadProvider() { }
    }
}
