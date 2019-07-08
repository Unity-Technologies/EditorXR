#if UNITY_2018_3_OR_NEWER
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [ModuleBehaviorCallbackOrder(ModuleOrders.UIModuleBehaviorOrder)]
    class EditorXRUIModule : ScriptableSettings<EditorXRUIModule>, IModuleDependency<EditorXRViewerModule>,
        IModuleDependency<EditorXRRayModule>, IModuleDependency<KeyboardModule>, IInterfaceConnector, IUsesConnectInterfaces,
        IDelayedInitializationModule,IModuleBehaviorCallbacks, IUsesFunctionalityInjection, IProvidesSetManipulatorsVisible,
        IProvidesRequestStencilRef, IProvidesGetManipulatorDragState
    {
        const byte k_MinStencilRef = 2;

#pragma warning disable 649
        [SerializeField]
        Camera m_EventCameraPrefab;
#pragma warning restore 649

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

        readonly List<IManipulatorController> m_ManipulatorControllers = new List<IManipulatorController>();
        readonly HashSet<IUsesSetManipulatorsVisible> m_ManipulatorsHiddenRequests = new HashSet<IUsesSetManipulatorsVisible>();
        EditorXRViewerModule m_ViewerModule;
        EditorXRRayModule m_RayModule;
        KeyboardModule m_KeyboardModule;

        Transform m_ModuleParent;
        GameObject m_NewEventSystem;
        MultipleRayInputModule m_NewInputModule;

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }
        public int connectInterfaceOrder { get { return 0; } }

        //TODO: Expose input via FI
        internal MultipleRayInputModule InputModule { get; private set; }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        public void ConnectDependency(EditorXRViewerModule dependency)
        {
            m_ViewerModule = dependency;
        }

        public void ConnectDependency(EditorXRRayModule dependency)
        {
            m_RayModule = dependency;
        }

        public void ConnectDependency(KeyboardModule dependency)
        {
            m_KeyboardModule = dependency;
        }

        public void LoadModule()
        {
            IInstantiateUIMethods.instantiateUI = InstantiateUI;

            m_ModuleParent = ModuleLoaderCore.instance.GetModuleParent().transform;
        }

        public void UnloadModule() { }

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
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var manipulatorController = target as IManipulatorController;
            if (manipulatorController != null)
                m_ManipulatorControllers.Remove(manipulatorController);
        }

        public void Initialize()
        {
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem)
            {
                InputModule = eventSystem.GetComponent<MultipleRayInputModule>();
                if (!InputModule)
                {
                    m_NewInputModule = eventSystem.gameObject.AddComponent<MultipleRayInputModule>();
                    InputModule = m_NewInputModule;
                }
            }
            else
            {
                m_NewEventSystem = new GameObject("EventSystem");
                InputModule = m_NewEventSystem.AddComponent<MultipleRayInputModule>();
            }

            this.InjectFunctionalitySingle(InputModule);
            this.ConnectInterfaces(InputModule);

            var customPreviewCamera = m_ViewerModule.customPreviewCamera;
            if (customPreviewCamera != null)
                InputModule.layerMask |= customPreviewCamera.hmdOnlyLayerMask;

            InputModule.preProcessRaycastSource = m_RayModule.PreProcessRaycastSource;

            m_EventCamera = EditorXRUtils.Instantiate(m_EventCameraPrefab.gameObject, m_ModuleParent).GetComponent<Camera>();
            m_EventCamera.enabled = false;
            InputModule.eventCamera = m_EventCamera;
        }

        public void Shutdown()
        {
            if (m_EventCamera)
                UnityObjectUtils.Destroy(m_EventCamera.gameObject);

            if (m_NewInputModule)
                DestroyImmediate(m_NewInputModule);

            if (m_NewEventSystem)
                DestroyImmediate(m_NewEventSystem);
        }

        internal GameObject InstantiateUI(GameObject prefab, Transform parent = null, bool worldPositionStays = true, Transform rayOrigin = null)
        {
            var go = EditorXRUtils.Instantiate(prefab, parent ? parent : m_ModuleParent, worldPositionStays);
            foreach (var canvas in go.GetComponentsInChildren<Canvas>())
                canvas.worldCamera = m_EventCamera;

            foreach (var inputField in go.GetComponentsInChildren<InputField>())
            {
                if (inputField is NumericInputField)
                    inputField.spawnKeyboard = m_KeyboardModule.SpawnNumericKeyboard;
                else if (inputField is StandardInputField)
                    inputField.spawnKeyboard = m_KeyboardModule.SpawnAlphaNumericKeyboard;
            }

            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                this.ConnectInterfaces(mb, rayOrigin);
                this.InjectFunctionalitySingle(mb);
            }

            return go;
        }

        public void SetManipulatorsVisible(IUsesSetManipulatorsVisible setter, bool visible)
        {
            if (visible)
                m_ManipulatorsHiddenRequests.Remove(setter);
            else
                m_ManipulatorsHiddenRequests.Add(setter);
        }

        public bool GetManipulatorDragState()
        {
            foreach (var currentController in m_ManipulatorControllers)
            {
                if (currentController.manipulatorDragging)
                    return true;
            }

            return false;
        }

        internal void UpdateManipulatorVisibilities()
        {
            var manipulatorsVisible = m_ManipulatorsHiddenRequests.Count == 0;
            foreach (var controller in m_ManipulatorControllers)
            {
                controller.manipulatorVisible = manipulatorsVisible;
            }
        }

        public byte RequestStencilRef()
        {
            return stencilRef++;
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            UpdateManipulatorVisibilities();
        }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var manipulatorVisibilitySubscriber = obj as IFunctionalitySubscriber<IProvidesSetManipulatorsVisible>;
            if (manipulatorVisibilitySubscriber != null)
                manipulatorVisibilitySubscriber.provider = this;

            var requestStencilRefSubscriber = obj as IFunctionalitySubscriber<IProvidesRequestStencilRef>;
            if (requestStencilRefSubscriber != null)
                requestStencilRefSubscriber.provider = this;

            var getManipulatorDragStateSubscriber = obj as IFunctionalitySubscriber<IProvidesGetManipulatorDragState>;
            if (getManipulatorDragStateSubscriber != null)
                getManipulatorDragStateSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
#endif
