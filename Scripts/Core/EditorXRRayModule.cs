#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Manipulators;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    class EditorXRRayModule : MonoBehaviour, IModuleDependency<EditorVR>, IModuleDependency<HighlightModule>,
        IModuleDependency<IntersectionModule>, IModuleDependency<EditorXRMiniWorldModule>,
        IModuleDependency<DeviceInputModule>, IModuleDependency<MultipleRayInputModule>,
        IModuleDependency<KeyboardModule>, IModuleDependency<WorkspaceModule>, IModuleDependency<EditorXRViewerModule>,
        IModuleDependency<EditorXRDirectSelectionModule>, IModuleDependency<EditorXRUIModule>,
        IModuleDependency<EditorXRMenuModule>, IModuleDependency<EditorXRToolModule>,
        IInterfaceConnector, IForEachRayOrigin, IConnectInterfaces, IStandardIgnoreList
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
        EditorVR m_EditorVR;
        HighlightModule m_HighlightModule;
        EditorXRMiniWorldModule m_MiniWorldModule;
        IntersectionModule m_IntersectionModule;
        DeviceInputModule m_DeviceInputModule;
        MultipleRayInputModule m_MultipleRayInputModule;
        KeyboardModule m_KeyboardModule;
        WorkspaceModule m_WorkspaceModule;
        EditorXRViewerModule m_ViewerModule;
        EditorXRDirectSelectionModule m_DirectSelectionModule;
        EditorXRUIModule m_UIModule;
        EditorXRMenuModule m_MenuModule;
        EditorXRToolModule m_ToolModule;

        internal DefaultProxyRay proxyRayPrefab { get { return m_ProxyRayPrefab; } }

        internal Transform lastSelectionRayOrigin { get; private set; }

        public List<GameObject> ignoreList { private get; set; }

        public void ConnectDependency(HighlightModule dependency)
        {
            m_HighlightModule = dependency;
        }

        public void ConnectDependency(EditorXRMiniWorldModule dependency)
        {
            m_MiniWorldModule = dependency;
        }

        public void ConnectDependency(IntersectionModule dependency)
        {
            m_IntersectionModule = dependency;
            ignoreList = dependency.standardIgnoreList;
        }

        public void ConnectDependency(DeviceInputModule dependency)
        {
            m_DeviceInputModule = dependency;
        }

        public void ConnectDependency(MultipleRayInputModule dependency)
        {
            m_MultipleRayInputModule = dependency;
        }

        public void ConnectDependency(KeyboardModule dependency)
        {
            m_KeyboardModule = dependency;
        }

        public void ConnectDependency(WorkspaceModule dependency)
        {
            m_WorkspaceModule = dependency;
        }

        public void ConnectDependency(EditorXRViewerModule dependency)
        {
            m_ViewerModule = dependency;
        }

        public void ConnectDependency(EditorXRDirectSelectionModule dependency)
        {
            m_DirectSelectionModule = dependency;
        }

        public void ConnectDependency(EditorXRUIModule dependency)
        {
            m_UIModule = dependency;
        }

        public void ConnectDependency(EditorXRMenuModule dependency)
        {
            m_MenuModule = dependency;
        }

        public void ConnectDependency(EditorXRToolModule dependency)
        {
            m_ToolModule = dependency;
        }

        public void LoadModule()
        {
            ISetDefaultRayColorMethods.setDefaultRayColor = SetDefaultRayColor;
            IGetDefaultRayColorMethods.getDefaultRayColor = GetDefaultRayColor;

            IRayVisibilitySettingsMethods.removeRayVisibilitySettings = RemoveVisibilitySettings;
            IRayVisibilitySettingsMethods.addRayVisibilitySettings = AddVisibilitySettings;

            IForEachRayOriginMethods.forEachRayOrigin = IterateRayOrigins;
            IGetFieldGrabOriginMethods.getFieldGrabOriginForRayOrigin = GetFieldGrabOriginForRayOrigin;
            IGetPreviewOriginMethods.getPreviewOriginForRayOrigin = GetPreviewOriginForRayOrigin;
            IUsesRaycastResultsMethods.getFirstGameObject = GetFirstGameObject;
            IRayToNodeMethods.requestNodeFromRayOrigin = RequestNodeFromRayOrigin;
            INodeToRayMethods.requestRayOriginFromNode = RequestRayOriginFromNode;
            IGetRayVisibilityMethods.isRayVisible = IsRayActive;
            IGetRayVisibilityMethods.isConeVisible = IsConeActive;
        }

        public void UnloadModule()
        {
            foreach (var proxy in m_Proxies)
            {
                if (proxy == null || proxy as MonoBehaviour == null)
                    continue;

                UnityObjectUtils.Destroy(((MonoBehaviour)proxy).gameObject);
            }
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var rayOrigin = userData as Transform;
            if (rayOrigin)
            {
                var evrDeviceData = m_EditorVR.deviceData;

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

                var deviceData = evrDeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);

                var handedRay = target as IUsesNode;
                if (handedRay != null && deviceData != null)
                    handedRay.node = deviceData.node;
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
                    AddVisibilitySettings(rayOrigin, mainMenu, false, false);

                // Show the ray if the menu is not hidden but the custom menu is overriding it, and is also hidden
                else if ((mainMenu.menuHideFlags & MenuHideFlags.Hidden) == 0 || customMenu != null && customMenu.menuHideFlags != 0)
                    AddVisibilitySettings(rayOrigin, mainMenu, true, true, 1);
                else
                    RemoveVisibilitySettings(rayOrigin, mainMenu);
            }
        }

        void SetLastSelectionRayOrigin(Transform rayOrigin)
        {
            lastSelectionRayOrigin = rayOrigin;
        }

        static bool OverrideSelectObject(GameObject hoveredObject)
        {
            // The player head can hovered, but not selected (only directly manipulated)
            if (hoveredObject && hoveredObject.CompareTag(EditorVR.VRPlayerTag))
            {
                // Clear the selection so that we do not manipulate it when moving the player head
                Selection.activeObject = null;
                return true;
            }

            return false;
        }

        internal void CreateAllProxies()
        {
            var cameraRig = CameraUtils.GetCameraRig();
            var proxyTypes = CollectionPool<List<Type>, Type>.GetCollection();
            typeof(IProxy).GetImplementationsOfInterface(proxyTypes);
            foreach (var proxyType in proxyTypes)
            {
                var proxy = (IProxy)EditorXRUtils.CreateGameObjectWithComponent(proxyType, cameraRig, false);
                this.ConnectInterfaces(proxy);
                proxy.trackedObjectInput = m_DeviceInputModule.trackedObjectInput;
                proxy.activeChanged += () => OnProxyActiveChanged(proxy);

                m_Proxies.Add(proxy);
            }

            CollectionPool<List<Type>, Type>.RecycleCollection(proxyTypes);
        }

        void OnProxyActiveChanged(IProxy proxy)
        {
            proxy.hidden = !proxy.active;

            if (proxy.active)
            {
                var evrDeviceData = m_EditorVR.deviceData;
                if (!evrDeviceData.Any(dd => dd.proxy == proxy))
                {
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
                                var deviceData = new DeviceData();
                                evrDeviceData.Add(deviceData);
                                deviceData.proxy = proxy;
                                deviceData.node = node;
                                deviceData.rayOrigin = rayOrigin;
                                deviceData.inputDevice = device;

                                // Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
                                m_MultipleRayInputModule.AddRaycastSource(proxy, node, rayOrigin, source =>
                                {
                                    // Do not invalidate UI raycasts in the middle of a drag operation
                                    if (!source.draggedObject)
                                    {
                                        var sourceRayOrigin = source.rayOrigin;
                                        if (m_DirectSelectionModule.IsHovering(sourceRayOrigin))
                                            return false;

                                        var hoveredObject = source.hoveredObject;

                                        // The manipulator needs rays to go through scene objects in order to work
                                        var isManipulator = hoveredObject && hoveredObject.GetComponentInParent<IManipulator>() != null;
                                        float sceneObjectDistance;
                                        var raycastObject = m_IntersectionModule.GetFirstGameObject(sourceRayOrigin, out sceneObjectDistance);
                                        var uiDistance = source.eventData.pointerCurrentRaycast.distance;

                                        // If the distance to a scene object is less than the distance to the hovered UI, invalidate the UI raycast
                                        if (!isManipulator && raycastObject && sceneObjectDistance < uiDistance && !ignoreList.Contains(raycastObject))
                                            return false;
                                    }

                                    if (!m_MenuModule.IsValidHover(source))
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
                        dpr.SetColor(m_HighlightModule.highlightColor);
                        m_DefaultRays.Add(rayOrigin, dpr);

                        m_KeyboardModule.SpawnKeyboardMallet(rayOrigin);

                        var proxyExtras = m_ProxyExtras;
                        if (proxyExtras)
                        {
                            var extraData = proxyExtras.data;
                            List<GameObject> prefabs;
                            if (extraData.TryGetValue(rayOriginPair.Key, out prefabs))
                            {
                                foreach (var prefab in prefabs)
                                {
                                    var go = m_UIModule.InstantiateUI(prefab);
                                    go.transform.SetParent(rayOriginPair.Value, false);
                                }
                            }
                        }

                        var tester = rayOriginPair.Value.GetComponentInChildren<IntersectionTester>();
                        tester.active = proxy.active;
                        m_IntersectionModule.AddTester(tester);

                        m_HighlightModule.AddRayOriginForNode(node, rayOrigin);

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

                    m_ToolModule.SpawnDefaultTools(proxy);
                }
            }
        }

        internal void UpdateRaycasts()
        {
            var distance = k_DefaultRayLength * m_ViewerModule.GetViewerScale();
            foreach (var deviceData in m_EditorVR.deviceData)
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

                    var distance = k_DefaultRayLength *  m_ViewerModule.GetViewerScale();

                    // Give UI priority over scene objects (e.g. For the TransformTool, handles are generally inside of the
                    // object, so visually show the ray terminating there instead of the object; UI is already given
                    // priority on the input side)
                    var uiEventData = m_MultipleRayInputModule.GetPointerEventData(rayOrigin);
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
            var evrDeviceData = m_EditorVR.deviceData;
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
            var evrDeviceData = m_EditorVR.deviceData;
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
            var deviceData = m_EditorVR.deviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
            if (deviceData != null)
                result = deviceData.proxy;

            return result;
        }

        GameObject GetFirstGameObject(Transform rayOrigin)
        {
            float distance;
            var go = m_IntersectionModule.GetFirstGameObject(rayOrigin, out distance);
            if (go)
                return go;

            // If a raycast did not find an object use the spatial hash as a final test
            var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();
            Vector3 collisionPoint;
            var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester, out collisionPoint);
            if (renderer && !renderer.CompareTag(EditorVR.VRPlayerTag))
                return renderer.gameObject;

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

        Transform GetPreviewOriginForRayOrigin(Transform rayOrigin)
        {
            foreach (var proxy in m_Proxies)
            {
                Transform previewOrigin;
                if (proxy.previewOrigins.TryGetValue(rayOrigin, out previewOrigin))
                    return previewOrigin;
            }

            return null;
        }

        Transform GetFieldGrabOriginForRayOrigin(Transform rayOrigin)
        {
            foreach (var proxy in m_Proxies)
            {
                Transform fieldGrabOrigins;
                if (proxy.fieldGrabOrigins.TryGetValue(rayOrigin, out fieldGrabOrigins))
                    return fieldGrabOrigins;
            }

            return null;
        }

        static bool IsRayActive(Transform rayOrigin)
        {
            var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
            return dpr == null || dpr.rayVisible;
        }

        static bool IsConeActive(Transform rayOrigin)
        {
            var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
            return dpr == null || dpr.coneVisible;
        }

        internal void AddVisibilitySettings(Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0)
        {
            if (rayOrigin)
            {
                var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                if (dpr)
                    dpr.AddVisibilitySettings(caller, rayVisible, coneVisible, priority);
            }
        }

        internal void RemoveVisibilitySettings(Transform rayOrigin, object obj)
        {
            if (!rayOrigin) // Prevent MissingReferenceException on closing m_EditorVR
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
                m_StandardManipulator = m_EditorVR.GetComponentInChildren<StandardManipulator>(true);
                if (m_StandardManipulator)
                    ConnectInterface(m_StandardManipulator);
            }

            if (m_StandardManipulator)
                m_StandardManipulator.AdjustScale(cameraPosition, matrix);

            if (!m_ScaleManipulator)
                m_ScaleManipulator = m_EditorVR.GetComponentInChildren<ScaleManipulator>(true);

            if (m_ScaleManipulator)
                m_ScaleManipulator.AdjustScale(cameraPosition, matrix);
        }

        Node RequestNodeFromRayOrigin(Transform rayOrigin)
        {
            if (rayOrigin == null)
                return Node.None;

            foreach (var deviceData in m_EditorVR.deviceData)
            {
                if (!deviceData.proxy.active)
                    continue;

                if (deviceData.rayOrigin == rayOrigin)
                    return deviceData.node;
            }

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

            foreach (var deviceData in m_EditorVR.deviceData)
            {
                if (!deviceData.proxy.active)
                    continue;

                if (deviceData.node == node)
                    return deviceData.rayOrigin;
            }

            foreach (var kvp in m_MiniWorldModule.rays)
            {
                if (kvp.Value.node == node)
                    return kvp.Value.originalRayOrigin;
            }

            return null;
        }

        void SetDefaultRayColor(Transform rayOrigin, Color color)
        {
            if (rayOrigin)
            {
                var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                if (dpr)
                    dpr.SetColor(color);
            }

            m_HighlightModule.highlightColor = color;
        }

        Color GetDefaultRayColor(Transform rayOrigin)
        {
            if (rayOrigin)
            {
                var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                if (dpr)
                    return dpr.GetColor();
            }

            return m_HighlightModule.highlightColor;
        }

        public void ConnectDependency(EditorVR dependency)
        {
            m_EditorVR = dependency;
        }
    }
}
#endif
