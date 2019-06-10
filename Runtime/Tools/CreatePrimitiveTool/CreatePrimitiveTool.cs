using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    [MainMenuItem("Primitive", "Create", "Create primitives in the scene")]
    [SpatialMenuItem("Primitives", "Tools", "Create primitives in the scene")]
    sealed class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IUsesConnectInterfaces, IUsesInstantiateMenuUI,
        IUsesRayOrigin, IUsesSpatialHash, IUsesViewerScale, IUsesSelectTool, IUsesIsHoveringOverUI, IUsesIsMainMenuVisible,
        IUsesRayVisibilitySettings, IMenuIcon, IUsesRequestFeedback, IUsesNode
    {
#pragma warning disable 649
        [SerializeField]
        CreatePrimitiveMenu m_MenuPrefab;

        [SerializeField]
        Sprite m_Icon;
#pragma warning restore 649

        const float k_DrawDistance = 0.075f;

        GameObject m_ToolMenu;

        PrimitiveType m_SelectedPrimitiveType = PrimitiveType.Cube;
        bool m_Freeform;

        GameObject m_CurrentGameObject;

        Vector3 m_StartPoint = Vector3.zero;
        Vector3 m_EndPoint = Vector3.zero;

        PrimitiveCreationStates m_State = PrimitiveCreationStates.StartPoint;

        public Transform rayOrigin { get; set; }
        public Node node { get; set; }

        public Sprite icon { get { return m_Icon; } }

        enum PrimitiveCreationStates
        {
            StartPoint,
            EndPoint,
            Freeform
        }

#if !FI_AUTOFILL
        IProvidesSpatialHash IFunctionalitySubscriber<IProvidesSpatialHash>.provider { get; set; }
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
        IProvidesSelectTool IFunctionalitySubscriber<IProvidesSelectTool>.provider { get; set; }
        IProvidesRequestFeedback IFunctionalitySubscriber<IProvidesRequestFeedback>.provider { get; set; }
        IProvidesRayVisibilitySettings IFunctionalitySubscriber<IProvidesRayVisibilitySettings>.provider { get; set; }
        IProvidesIsMainMenuVisible IFunctionalitySubscriber<IProvidesIsMainMenuVisible>.provider { get; set; }
        IProvidesIsHoveringOverUI IFunctionalitySubscriber<IProvidesIsHoveringOverUI>.provider { get; set; }
        IProvidesInstantiateMenuUI IFunctionalitySubscriber<IProvidesInstantiateMenuUI>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        void Start()
        {
            // Clear selection so we can't manipulate things
            Selection.activeGameObject = null;

            m_ToolMenu = this.InstantiateMenuUI(rayOrigin, m_MenuPrefab);
            var createPrimitiveMenu = m_ToolMenu.GetComponent<CreatePrimitiveMenu>();
            this.ConnectInterfaces(createPrimitiveMenu, rayOrigin);
            createPrimitiveMenu.selectPrimitive = SetSelectedPrimitive;
            createPrimitiveMenu.close = Close;

            var controls = new BindingDictionary();
            InputUtils.GetBindingDictionaryFromActionMap(standardActionMap, controls);

            foreach (var control in controls)
            {
                foreach (var id in control.Value)
                {
                    var request = this.GetFeedbackRequestObject<ProxyFeedbackRequest>(this);
                    request.node = node;
                    request.control = id;
                    request.tooltipText = "Draw";
                    this.AddFeedbackRequest(request);
                }
            }
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            if (!IsActive())
                return;

            var standardInput = (Standard)input;

            switch (m_State)
            {
                case PrimitiveCreationStates.StartPoint:
                {
                    HandleStartPoint(standardInput, consumeControl);
                    break;
                }
                case PrimitiveCreationStates.EndPoint:
                {
                    UpdatePositions();
                    SetScalingForObjectType();
                    CheckForTriggerRelease(standardInput, consumeControl);
                    break;
                }
                case PrimitiveCreationStates.Freeform:
                {
                    UpdatePositions();
                    UpdateFreeformScale();
                    CheckForTriggerRelease(standardInput, consumeControl);
                    break;
                }
            }

            if (m_State == PrimitiveCreationStates.StartPoint && this.IsHoveringOverUI(rayOrigin))
                this.RemoveRayVisibilitySettings(rayOrigin, this);
            else
                this.AddRayVisibilitySettings(rayOrigin, this, false, true);
        }

        void SetSelectedPrimitive(PrimitiveType type, bool isFreeform)
        {
            m_SelectedPrimitiveType = type;
            m_Freeform = isFreeform;
        }

        void HandleStartPoint(Standard standardInput, ConsumeControlDelegate consumeControl)
        {
            if (standardInput.action.wasJustPressed)
            {
                m_CurrentGameObject = GameObject.CreatePrimitive(m_SelectedPrimitiveType);
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(m_CurrentGameObject, "Create Primitive");
#endif
                // Set starting minimum scale (don't allow zero scale object to be created)
                const float kMinScale = 0.0025f;
                var viewerScale = this.GetViewerScale();
                m_CurrentGameObject.transform.localScale = kMinScale * viewerScale * Vector3.one;
                m_StartPoint = rayOrigin.position + k_DrawDistance * viewerScale * rayOrigin.forward;
                m_CurrentGameObject.transform.position = m_StartPoint;

                m_State = m_Freeform ? PrimitiveCreationStates.Freeform : PrimitiveCreationStates.EndPoint;

                this.AddToSpatialHash(m_CurrentGameObject);

                consumeControl(standardInput.action);
                Selection.activeGameObject = m_CurrentGameObject;
            }
        }

        void SetScalingForObjectType()
        {
            var corner = (m_EndPoint - m_StartPoint).magnitude;

            // it feels better to scale these primitives vertically with the draw point
            if (m_SelectedPrimitiveType == PrimitiveType.Capsule || m_SelectedPrimitiveType == PrimitiveType.Cylinder || m_SelectedPrimitiveType == PrimitiveType.Cube)
                m_CurrentGameObject.transform.localScale = corner * 0.5f * Vector3.one;
            else
                m_CurrentGameObject.transform.localScale = Vector3.one * corner;
        }

        void UpdatePositions()
        {
            m_EndPoint = rayOrigin.position + k_DrawDistance * this.GetViewerScale() * rayOrigin.forward;
            m_CurrentGameObject.transform.position = (m_StartPoint + m_EndPoint) * 0.5f;
        }

        void UpdateFreeformScale()
        {
            var maxCorner = Vector3.Max(m_StartPoint, m_EndPoint);
            var minCorner = Vector3.Min(m_StartPoint, m_EndPoint);
            m_CurrentGameObject.transform.localScale = maxCorner - minCorner;
        }

        void CheckForTriggerRelease(Standard standardInput, ConsumeControlDelegate consumeControl)
        {
            // Ready for next object to be created
            if (standardInput.action.wasJustReleased)
            {
                m_State = PrimitiveCreationStates.StartPoint;
#if UNITY_EDITOR
                Undo.IncrementCurrentGroup();
#endif

                consumeControl(standardInput.action);
            }
        }

        bool IsActive()
        {
            return !this.IsMainMenuVisible(rayOrigin);
        }

        void Close()
        {
            this.SelectTool(rayOrigin, GetType());
        }

        void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_ToolMenu);

            if (rayOrigin == null)
                return;

            this.RemoveRayVisibilitySettings(rayOrigin, this);
            this.ClearFeedbackRequests(this);
        }

        public ActionMap standardActionMap { private get; set; }
    }
}
