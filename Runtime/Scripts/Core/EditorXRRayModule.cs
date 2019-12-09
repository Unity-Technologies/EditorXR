using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.EditorXR.Helpers;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Manipulators;
using Unity.Labs.EditorXR.Modules;
using Unity.Labs.EditorXR.Proxies;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR.Core
{
    class EditorXRRayModule : ScriptableSettings<EditorXRRayModule>,
        IModuleDependency<DeviceInputModule>, IInterfaceConnector, IForEachRayOrigin,
        IUsesConnectInterfaces, IStandardIgnoreList, IDelayedInitializationModule, ISelectionChanged,
        IModuleBehaviorCallbacks, IUsesFunctionalityInjection, IProvidesRaycastResults, IProvidesSetDefaultRayColor,
        IProvidesGetDefaultRayColor, IProvidesRayVisibilitySettings, IProvidesGetRayVisibility, IProvidesGetPreviewOrigin,
        IProvidesGetFieldGrabOrigin, IInstantiateUI, IUsesViewerScale, IUsesAddRaycastSource, IUsesGetRayEventData
    {
        internal delegate void ForEachProxyDeviceCallback(DeviceData deviceData);

        const float k_DefaultRayLength = 100f;

#pragma warning disable 649
        [SerializeField]
        DefaultProxyRay m_ProxyRayPrefab;

        [SerializeField]
        ProxyExtras m_ProxyExtras;
#pragma warning restore 649

        internal Dictionary<Transform, DefaultProxyRay> defaultRays
        {
            get { return m_DefaultRays; }
        }

        readonly Dictionary<Transform, DefaultProxyRay> m_DefaultRays = new Dictionary<Transform, DefaultProxyRay>();

        readonly List<IProxy> m_Proxies = new List<IProxy>();

        StandardManipulator m_StandardManipulator;
        ScaleManipulator m_ScaleManipulator;
        EditorXRMiniWorldModule m_MiniWorldModule;
        IntersectionModule m_IntersectionModule;
        DeviceInputModule m_DeviceInputModule;
        WorkspaceModule m_WorkspaceModule;
        EditorXRDirectSelectionModule m_DirectSelectionModule;
        EditorXRMenuModule m_MenuModule;
        EditorXRToolModule m_ToolModule;
        SerializedPreferencesModule m_SerializedPreferences;

        Transform m_ModuleParent;

        internal DefaultProxyRay proxyRayPrefab { get { return m_ProxyRayPrefab; } }

        internal Transform lastSelectionRayOrigin { get; private set; }

        public List<GameObject> ignoreList { private get; set; }

        public int initializationOrder { get { return 1; } }
        public int shutdownOrder { get { return 0; } }
        public int connectInterfaceOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
        IProvidesAddRaycastSource IFunctionalitySubscriber<IProvidesAddRaycastSource>.provider { get; set; }
        IProvidesGetRayEventData IFunctionalitySubscriber<IProvidesGetRayEventData>.provider { get; set; }
#endif

        public void ConnectDependency(DeviceInputModule dependency)
        {
            m_DeviceInputModule = dependency;
        }

        public void LoadModule()
        {
            IForEachRayOriginMethods.forEachRayOrigin = IterateRayOrigins;
            IRayToNodeMethods.requestNodeFromRayOrigin = RequestNodeFromRayOrigin;
            INodeToRayMethods.requestRayOriginFromNode = RequestRayOriginFromNode;

            var moduleLoaderCore = ModuleLoaderCore.instance;
            m_ToolModule = moduleLoaderCore.GetModule<EditorXRToolModule>();
            m_WorkspaceModule = moduleLoaderCore.GetModule<WorkspaceModule>();
            m_MenuModule = moduleLoaderCore.GetModule<EditorXRMenuModule>();
            m_MiniWorldModule = moduleLoaderCore.GetModule<EditorXRMiniWorldModule>();
            m_DirectSelectionModule = moduleLoaderCore.GetModule<EditorXRDirectSelectionModule>();
            m_SerializedPreferences = moduleLoaderCore.GetModule<SerializedPreferencesModule>();
            m_IntersectionModule = moduleLoaderCore.GetModule<IntersectionModule>();
            if (m_IntersectionModule != null)
                ignoreList = m_IntersectionModule.standardIgnoreList;

            m_ModuleParent = moduleLoaderCore.GetModuleParent().transform;
        }

        public void UnloadModule()
        {
            CleanupProxies();
        }

        void CleanupProxies()
        {
            foreach (var proxy in m_Proxies)
            {
                var behavior = proxy as MonoBehaviour;
                if (behavior == null)
                    continue;

                this.DisconnectInterfaces(proxy);
                UnityObjectUtils.Destroy(behavior.gameObject);
            }

            m_Proxies.Clear();
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var rayOrigin = userData as Transform;
            if (rayOrigin)
            {
                var deviceData = m_ToolModule.deviceData;

                var ray = target as IUsesRayOrigin;
                if (ray != null)
                    ray.rayOrigin = rayOrigin;

                var rayOrigins = target as IUsesRayOrigins;
                if (rayOrigins != null)
                {
                    List<Transform> otherRayOrigins = new List<Transform>();
                    this.ForEachRayOrigin(ro =>
                    {
                        if (ro != rayOrigin)
                            otherRayOrigins.Add(ro);
                    });
                    rayOrigins.otherRayOrigins = otherRayOrigins;
                }

                var firstDeviceData = deviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);

                var handedRay = target as IUsesNode;
                if (handedRay != null && firstDeviceData != null)
                    handedRay.node = firstDeviceData.node;
            }

            var selectionModule = target as SelectionModule;
            if (selectionModule)
            {
                selectionModule.selected += SetLastSelectionRayOrigin; // when a selection occurs in the selection tool, call show in the alternate menu, allowing it to show/hide itself.
                selectionModule.overrideSelectObject = OverrideSelectObject;
            }
        }

        public void DisconnectInterface(object target, object userData = null) { }

        internal void UpdateRayForDevice(DeviceData deviceData, Transform rayOrigin)
        {
            var mainMenu = deviceData.mainMenu;
            var customMenu = deviceData.customMenu;

            if (mainMenu != null)
            {
                // Hide the cone and ray if the main menu or custom menu are open
                if (mainMenu.menuHideFlags == 0 || customMenu != null && customMenu.menuHideFlags == 0)
                    AddRayVisibilitySettings(rayOrigin, mainMenu, false, false);

                // Show the ray if the menu is not hidden but the custom menu is overriding it, and is also hidden
                else if ((mainMenu.menuHideFlags & MenuHideFlags.Hidden) == 0 || customMenu != null && customMenu.menuHideFlags != 0)
                    AddRayVisibilitySettings(rayOrigin, mainMenu, true, true, 1);
                else
                    RemoveRayVisibilitySettings(rayOrigin, mainMenu);
            }
        }

        void SetLastSelectionRayOrigin(Transform rayOrigin)
        {
            lastSelectionRayOrigin = rayOrigin;
        }

        static bool OverrideSelectObject(GameObject hoveredObject)
        {
            // The player head can hovered, but not selected (only directly manipulated)
            if (hoveredObject && hoveredObject.CompareTag(EditorXR.VRPlayerTag))
            {
                // Clear the selection so that we do not manipulate it when moving the player head
                Selection.activeObject = null;
                return true;
            }

            return false;
        }

        public void Initialize()
        {
            var cameraRig = CameraUtils.GetCameraRig();
            var proxyTypes = CollectionPool<List<Type>, Type>.GetCollection();
            typeof(IProxy).GetImplementationsOfInterface(proxyTypes);
            foreach (var proxyType in proxyTypes)
            {
                var proxy = (IProxy)EditorXRUtils.CreateGameObjectWithComponent(proxyType, cameraRig, false);
                this.ConnectInterfaces(proxy);
                this.InjectFunctionalitySingle(proxy);
                var trackedObjectInput = m_DeviceInputModule.trackedObjectInput;
                if (trackedObjectInput == null)
                    Debug.LogError("Device Input Module not initialized--trackedObjectInput is null");

                proxy.trackedObjectInput = trackedObjectInput;
                proxy.activeChanged += () => OnProxyActiveChanged(proxy);

                m_Proxies.Add(proxy);
            }

            CollectionPool<List<Type>, Type>.RecycleCollection(proxyTypes);
        }

        public void Shutdown()
        {
            CleanupProxies();
        }

        void OnProxyActiveChanged(IProxy proxy)
        {
            proxy.hidden = !proxy.active;

            if (proxy.active)
            {
                var deviceData = m_ToolModule.deviceData;
                if (!deviceData.Any(dd => dd.proxy == proxy))
                {
                    var moduleLoaderCore = ModuleLoaderCore.instance;
                    var keyboardModule = moduleLoaderCore.GetModule<KeyboardModule>();
                    var highlightModule = moduleLoaderCore.GetModule<HighlightModule>();
                    foreach (var rayOriginPair in proxy.rayOrigins)
                    {
                        var node = rayOriginPair.Key;
                        var rayOrigin = rayOriginPair.Value;

                        var systemDevices = m_DeviceInputModule.GetSystemDevices();
                        for (int j = 0; j < systemDevices.Count; j++)
                        {
                            var device = systemDevices[j];

                            // Find device tagged with the node that matches this RayOrigin node
                            var deviceNode = m_DeviceInputModule.GetDeviceNode(device);
                            if (deviceNode == node)
                            {
                                var newDeviceData = new DeviceData();
                                deviceData.Add(newDeviceData);
                                newDeviceData.proxy = proxy;
                                newDeviceData.node = node;
                                newDeviceData.rayOrigin = rayOrigin;
                                newDeviceData.inputDevice = device;

                                if (!this.HasProvider<IProvidesAddRaycastSource>())
                                    continue;

                                // Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
                                this.AddRaycastSource(proxy, node, rayOrigin, source =>
                                {
                                    // Do not invalidate UI raycasts in the middle of a drag operation
                                    var eventData = source.eventData;
                                    if (!eventData.pointerDrag)
                                    {
                                        var sourceRayOrigin = source.rayOrigin;
                                        if (m_DirectSelectionModule != null && m_DirectSelectionModule.IsHovering(sourceRayOrigin))
                                            return false;

                                        var currentRaycast = eventData.pointerCurrentRaycast;
                                        var hoveredObject = currentRaycast.gameObject;

                                        // The manipulator needs rays to go through scene objects in order to work
                                        var isManipulator = hoveredObject && hoveredObject.GetComponentInParent<IManipulator>() != null;
                                        float sceneObjectDistance;
                                        var raycastObject = m_IntersectionModule.GetFirstGameObject(sourceRayOrigin, out sceneObjectDistance);
                                        var uiDistance = currentRaycast.distance;

                                        // If the distance to a scene object is less than the distance to the hovered UI, invalidate the UI raycast
                                        if (!isManipulator && raycastObject && sceneObjectDistance < uiDistance && !ignoreList.Contains(raycastObject))
                                            return false;
                                    }

                                    if (m_MenuModule != null && !m_MenuModule.IsValidHover(source))
                                        return false;

                                    // Proceed only for raycast sources that haven't been blocked via IBlockUIInteraction
                                    if (source.blocked)
                                        return false;

                                    return true;
                                });
                            }
                        }

                        rayOrigin.name = string.Format("{0} Ray Origin", node);
                        var rayTransform = EditorXRUtils.Instantiate(m_ProxyRayPrefab.gameObject, rayOrigin, false).transform;
                        rayTransform.position = rayOrigin.position;
                        rayTransform.rotation = rayOrigin.rotation;
                        var dpr = rayTransform.GetComponent<DefaultProxyRay>();
                        this.InjectFunctionalitySingle(dpr);
                        m_DefaultRays.Add(rayOrigin, dpr);
                        if (highlightModule != null)
                        {
                            dpr.SetColor(highlightModule.highlightColor);
                            highlightModule.AddRayOriginForNode(node, rayOrigin);
                        }

                        if(keyboardModule != null)
                            keyboardModule.SpawnKeyboardMallet(rayOrigin);

                        var proxyExtras = m_ProxyExtras;
                        if (proxyExtras)
                        {
                            var extraData = proxyExtras.data;
                            List<GameObject> prefabs;
                            if (extraData.TryGetValue(rayOriginPair.Key, out prefabs))
                            {
                                foreach (var prefab in prefabs)
                                {
                                    var go = this.InstantiateUI(prefab);
                                    go.transform.SetParent(rayOriginPair.Value, false);
                                }
                            }
                        }

                        var tester = rayOriginPair.Value.GetComponentInChildren<IntersectionTester>();
                        tester.active = proxy.active;
                        m_IntersectionModule.AddTester(tester);

                        if (m_WorkspaceModule != null)
                        {
                            switch (node)
                            {
                                case Node.LeftHand:
                                    m_WorkspaceModule.leftRayOrigin = rayOrigin;
                                    break;
                                case Node.RightHand:
                                    m_WorkspaceModule.rightRayOrigin = rayOrigin;
                                    break;
                            }
                        }
                    }

                    if (m_ToolModule != null)
                        m_ToolModule.SpawnDefaultTools(proxy);

                    if (m_SerializedPreferences != null)
                        m_SerializedPreferences.DeserializePreferences();

                    if (m_WorkspaceModule != null)
                        m_WorkspaceModule.CreateSerializedWorkspaces();
                }
            }
        }

        internal void UpdateRaycasts()
        {
            var distance = k_DefaultRayLength * this.GetViewerScale();
            foreach (var deviceData in m_ToolModule.deviceData)
            {
                var proxy = deviceData.proxy;
                if (!proxy.active)
                    continue;

                m_IntersectionModule.UpdateRaycast(deviceData.rayOrigin, distance);
            }
        }

        internal void UpdateDefaultProxyRays()
        {
            // Set ray lengths based on renderer bounds
            foreach (var proxy in m_Proxies)
            {
                if (!proxy.active)
                    continue;

                foreach (var kvp in proxy.rayOrigins)
                {
                    var rayOrigin = kvp.Value;
                    if (!rayOrigin)
                    {
                        Debug.Log("null rayorigin in " + proxy);
                        continue;
                    }

                    var distance = k_DefaultRayLength * this.GetViewerScale();

                    // Give UI priority over scene objects (e.g. For the TransformTool, handles are generally inside of the
                    // object, so visually show the ray terminating there instead of the object; UI is already given
                    // priority on the input side)
                    var uiEventData = this.GetRayEventData(rayOrigin);
                    if (uiEventData != null && uiEventData.pointerCurrentRaycast.isValid)
                    {
                        // Set ray length to distance to UI objects
                        distance = uiEventData.pointerCurrentRaycast.distance;
                    }
                    else
                    {
                        float hitDistance;
                        if (m_IntersectionModule.GetFirstGameObject(rayOrigin, out hitDistance))
                            distance = hitDistance;
                    }

                    m_DefaultRays[rayOrigin].SetLength(distance);
                }
            }
        }

        internal void ForEachProxyDevice(ForEachProxyDeviceCallback callback, bool activeOnly = true)
        {
            var evrDeviceData = m_ToolModule.deviceData;
            for (var i = 0; i < evrDeviceData.Count; i++)
            {
                var deviceData = evrDeviceData[i];
                var proxy = deviceData.proxy;
                if (activeOnly && !proxy.active)
                    continue;

                callback(deviceData);
            }
        }

        void IterateRayOrigins(ForEachRayOriginCallback callback)
        {
            var evrDeviceData = m_ToolModule.deviceData;
            for (var i = 0; i < evrDeviceData.Count; i++)
            {
                var deviceData = evrDeviceData[i];
                var proxy = deviceData.proxy;
                if (!proxy.active)
                    continue;

                callback(deviceData.rayOrigin);
            }
        }

        internal IProxy GetProxyForRayOrigin(Transform rayOrigin)
        {
            IProxy result = null;
            var deviceData = m_ToolModule.deviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
            if (deviceData != null)
                result = deviceData.proxy;

            return result;
        }

        public GameObject GetFirstGameObject(Transform rayOrigin)
        {
            float distance;
            var go = m_IntersectionModule.GetFirstGameObject(rayOrigin, out distance);
            if (go)
                return go;

            // If a raycast did not find an object use the spatial hash as a final test
            var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();
            Vector3 collisionPoint;
            var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester, out collisionPoint);
            if (renderer && !renderer.CompareTag(EditorXR.VRPlayerTag))
                return renderer.gameObject;

            if (m_MiniWorldModule == null)
                return null;

            foreach (var kvp in m_MiniWorldModule.rays)
            {
                var miniWorldRay = kvp.Value;
                if (miniWorldRay.originalRayOrigin.Equals(rayOrigin))
                {
                    tester = miniWorldRay.tester;
                    if (!tester.active)
                        continue;

                    renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester, out collisionPoint);
                    if (renderer)
                        return renderer.gameObject;
                }
            }

            return null;
        }

        public Transform GetPreviewOriginForRayOrigin(Transform rayOrigin)
        {
            foreach (var proxy in m_Proxies)
            {
                Transform previewOrigin;
                if (proxy.previewOrigins.TryGetValue(rayOrigin, out previewOrigin))
                    return previewOrigin;
            }

            return null;
        }

        public Transform GetFieldGrabOriginForRayOrigin(Transform rayOrigin)
        {
            foreach (var proxy in m_Proxies)
            {
                Transform fieldGrabOrigins;
                if (proxy.fieldGrabOrigins.TryGetValue(rayOrigin, out fieldGrabOrigins))
                    return fieldGrabOrigins;
            }

            return null;
        }

        public bool IsRayVisible(Transform rayOrigin)
        {
            var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
            return dpr == null || dpr.rayVisible;
        }

        public bool IsConeVisible(Transform rayOrigin)
        {
            var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
            return dpr == null || dpr.coneVisible;
        }

        public void AddRayVisibilitySettings(Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0)
        {
            if (rayOrigin)
            {
                var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                if (dpr)
                    dpr.AddVisibilitySettings(caller, rayVisible, coneVisible, priority);
            }
        }

        public void RemoveRayVisibilitySettings(Transform rayOrigin, object obj)
        {
            if (!rayOrigin) // Prevent MissingReferenceException on closing m_EditorXR
                return;

            var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
            if (dpr)
                dpr.RemoveVisibilitySettings(obj);
        }

        internal void PreProcessRaycastSource(Transform rayOrigin)
        {
            var camera = CameraUtils.GetMainCamera();
            var cameraPosition = camera.transform.position;
            var matrix = camera.worldToCameraMatrix;

            // Include inactive children to avoid constantly polling for manipulators until first selection is made
            if (!m_StandardManipulator)
            {
                m_StandardManipulator = m_ModuleParent.GetComponentInChildren<StandardManipulator>(true);
                if (m_StandardManipulator)
                    this.ConnectInterfaces(m_StandardManipulator);
            }

            if (m_StandardManipulator)
                m_StandardManipulator.AdjustScale(cameraPosition, matrix);

            if (!m_ScaleManipulator)
                m_ScaleManipulator = m_ModuleParent.GetComponentInChildren<ScaleManipulator>(true);

            if (m_ScaleManipulator)
                m_ScaleManipulator.AdjustScale(cameraPosition, matrix);
        }

        Node RequestNodeFromRayOrigin(Transform rayOrigin)
        {
            if (rayOrigin == null)
                return Node.None;

            foreach (var deviceData in m_ToolModule.deviceData)
            {
                if (!deviceData.proxy.active)
                    continue;

                if (deviceData.rayOrigin == rayOrigin)
                    return deviceData.node;
            }

            if (m_MiniWorldModule == null)
                return Node.None;

            foreach (var kvp in m_MiniWorldModule.rays)
            {
                if (kvp.Key == rayOrigin)
                    return kvp.Value.node;
            }

            return Node.None;
        }

        Transform RequestRayOriginFromNode(Node node)
        {
            if (node == Node.None)
                return null;

            foreach (var deviceData in m_ToolModule.deviceData)
            {
                if (!deviceData.proxy.active)
                    continue;

                if (deviceData.node == node)
                    return deviceData.rayOrigin;
            }

            if (m_MiniWorldModule == null)
                return null;

            foreach (var kvp in m_MiniWorldModule.rays)
            {
                if (kvp.Value.node == node)
                    return kvp.Value.originalRayOrigin;
            }

            return null;
        }

        public void SetDefaultRayColor(Transform rayOrigin, Color color)
        {
            if (rayOrigin)
            {
                var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                if (dpr)
                    dpr.SetColor(color);
            }

            var highlightModule = ModuleLoaderCore.instance.GetModule<HighlightModule>();
            if (highlightModule != null)
                highlightModule.highlightColor = color;
        }

        public Color GetDefaultRayColor(Transform rayOrigin)
        {
            if (rayOrigin)
            {
                var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                if (dpr)
                    return dpr.GetColor();
            }

            var highlightModule = ModuleLoaderCore.instance.GetModule<HighlightModule>();
            if (highlightModule != null)
                return highlightModule.highlightColor;

            return default(Color);
        }

        public void OnSelectionChanged()
        {
            if (m_MenuModule != null)
                m_MenuModule.UpdateAlternateMenuOnSelectionChanged(lastSelectionRayOrigin);
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            UpdateRaycasts();

            UpdateDefaultProxyRays();
        }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var raycastResultsSubscriber = obj as IFunctionalitySubscriber<IProvidesRaycastResults>;
            if (raycastResultsSubscriber != null)
                raycastResultsSubscriber.provider = this;

            var setDefaultRayColorSubscriber = obj as IFunctionalitySubscriber<IProvidesSetDefaultRayColor>;
            if (setDefaultRayColorSubscriber != null)
                setDefaultRayColorSubscriber.provider = this;

            var getDefaultRayColorSubscriber = obj as IFunctionalitySubscriber<IProvidesGetDefaultRayColor>;
            if (getDefaultRayColorSubscriber != null)
                getDefaultRayColorSubscriber.provider = this;

            var visibilitySettingsSubscriber = obj as IFunctionalitySubscriber<IProvidesRayVisibilitySettings>;
            if (visibilitySettingsSubscriber != null)
                visibilitySettingsSubscriber.provider = this;

            var getVisibilitySubscriber = obj as IFunctionalitySubscriber<IProvidesGetRayVisibility>;
            if (getVisibilitySubscriber != null)
                getVisibilitySubscriber.provider = this;

            var getPreviewOriginSubscriber = obj as IFunctionalitySubscriber<IProvidesGetPreviewOrigin>;
            if (getPreviewOriginSubscriber != null)
                getPreviewOriginSubscriber.provider = this;

            var getFieldGrabOriginSubscriber = obj as IFunctionalitySubscriber<IProvidesGetFieldGrabOrigin>;
            if (getFieldGrabOriginSubscriber != null)
                getFieldGrabOriginSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
