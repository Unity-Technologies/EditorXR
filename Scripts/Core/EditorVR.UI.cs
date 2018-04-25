#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        [SerializeField]
        Camera m_EventCameraPrefab;

        [SerializeField]
        SpatialUIController m_SpatialUIPrefab;

        class UI : Nested, IInterfaceConnector, IConnectInterfaces
        {
            const byte k_MinStencilRef = 2;

            byte stencilRef
            {
                get { return m_StencilRef; }
                set
                {
                    m_StencilRef = (byte)Mathf.Clamp(value, k_MinStencilRef, byte.MaxValue);

                    // Wrap
                    if (m_StencilRef == byte.MaxValue)
                        m_StencilRef = k_MinStencilRef;
                }
            }

            byte m_StencilRef = k_MinStencilRef;

            Camera m_EventCamera;
            SpatialUIController m_SpatialUIController;

            readonly List<IManipulatorController> m_ManipulatorControllers = new List<IManipulatorController>();
            readonly HashSet<ISetManipulatorsVisible> m_ManipulatorsHiddenRequests = new HashSet<ISetManipulatorsVisible>();

            public UI()
            {
                IInstantiateUIMethods.instantiateUI = InstantiateUI;
                IRequestStencilRefMethods.requestStencilRef = RequestStencilRef;
                ISetManipulatorsVisibleMethods.setManipulatorsVisible = SetManipulatorsVisible;
                IGetManipulatorDragStateMethods.getManipulatorDragState = GetManipulatorDragState;
            }

            public void ConnectInterface(object target, object userData = null)
            {
                var manipulatorController = target as IManipulatorController;
                if (manipulatorController != null)
                    m_ManipulatorControllers.Add(manipulatorController);

                var usesStencilRef = target as IUsesStencilRef;
                if (usesStencilRef != null)
                {
                    byte? stencilRef = null;

                    var mb = target as MonoBehaviour;
                    if (mb)
                    {
                        var parent = mb.transform.parent;
                        if (parent)
                        {
                            // For workspaces and tools, it's likely that the stencil ref should be shared internally
                            var parentStencilRef = parent.GetComponentInParent<IUsesStencilRef>();
                            if (parentStencilRef != null)
                                stencilRef = parentStencilRef.stencilRef;
                        }
                    }

                    usesStencilRef.stencilRef = stencilRef ?? RequestStencilRef();
                }

                var spatialMenuProvider = target as ISpatialMenuProvider;
                if (spatialMenuProvider != null)
                    m_SpatialUIController.AddProvider(spatialMenuProvider);
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var manipulatorController = target as IManipulatorController;
                if (manipulatorController != null)
                    m_ManipulatorControllers.Remove(manipulatorController);
            }

            internal void Initialize()
            {
                // Create event system, input module, and event camera
                ObjectUtils.AddComponent<EventSystem>(evr.gameObject);

                var inputModule = evr.AddModule<MultipleRayInputModule>();

                var customPreviewCamera = evr.GetNestedModule<Viewer>().customPreviewCamera;
                if (customPreviewCamera != null)
                    inputModule.layerMask |= customPreviewCamera.hmdOnlyLayerMask;

                m_EventCamera = ObjectUtils.Instantiate(evr.m_EventCameraPrefab.gameObject, evr.transform).GetComponent<Camera>();
                m_EventCamera.enabled = false;
                inputModule.eventCamera = m_EventCamera;

                inputModule.preProcessRaycastSource = evr.GetNestedModule<Rays>().PreProcessRaycastSource;

                evr.AddModule<AdaptivePositionModule>();

                // SpatialUI must be instantiated after the AdaptivePositionModule is added
                m_SpatialUIController = ObjectUtils.Instantiate(evr.m_SpatialUIPrefab.gameObject).GetComponent<SpatialUIController>();
                this.ConnectInterfaces(m_SpatialUIController);
            }

            internal override void OnDestroy()
            {
                base.OnDestroy();

                ObjectUtils.Destroy(m_SpatialUIController.gameObject);
            }

            internal GameObject InstantiateUI(GameObject prefab, Transform parent = null, bool worldPositionStays = true, Transform rayOrigin = null)
            {
                var go = ObjectUtils.Instantiate(prefab, parent ? parent : evr.transform, worldPositionStays);
                foreach (var canvas in go.GetComponentsInChildren<Canvas>())
                    canvas.worldCamera = m_EventCamera;

                var keyboardModule = evr.GetModule<KeyboardModule>();
                foreach (var inputField in go.GetComponentsInChildren<InputField>())
                {
                    if (inputField is NumericInputField)
                        inputField.spawnKeyboard = keyboardModule.SpawnNumericKeyboard;
                    else if (inputField is StandardInputField)
                        inputField.spawnKeyboard = keyboardModule.SpawnAlphaNumericKeyboard;
                }

                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                    this.ConnectInterfaces(mb, rayOrigin);

                return go;
            }

            void SetManipulatorsVisible(ISetManipulatorsVisible setter, bool visible)
            {
                if (visible)
                    m_ManipulatorsHiddenRequests.Remove(setter);
                else
                    m_ManipulatorsHiddenRequests.Add(setter);
            }

            bool GetManipulatorDragState()
            {
                foreach (var currentController in m_ManipulatorControllers)
                {
                    if (currentController.manipulatorDragging)
                        return true;
                }

                return false;
            }

            internal void UpdateManipulatorVisibilites()
            {
                var manipulatorsVisible = m_ManipulatorsHiddenRequests.Count == 0;
                foreach (var controller in m_ManipulatorControllers)
                {
                    controller.manipulatorVisible = manipulatorsVisible;
                }
            }

            byte RequestStencilRef()
            {
                return stencilRef++;
            }
        }
    }
}
#endif
