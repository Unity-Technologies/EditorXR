using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Manipulators;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        [SerializeField]
        DefaultProxyRay m_ProxyRayPrefab;

        [SerializeField]
        ProxyExtras m_ProxyExtras;

        class Rays : Nested, IInterfaceConnector, IForEachRayOrigin, IConnectInterfaces, IStandardIgnoreList
        {
            internal delegate void ForEachProxyDeviceCallback(DeviceData deviceData);

            const float k_DefaultRayLength = 100f;

            internal Dictionary<Transform, DefaultProxyRay> defaultRays { get { return m_DefaultRays; } }

            readonly Dictionary<Transform, DefaultProxyRay> m_DefaultRays = new Dictionary<Transform, DefaultProxyRay>();

            readonly List<IProxy> m_Proxies = new List<IProxy>();

            StandardManipulator m_StandardManipulator;
            ScaleManipulator m_ScaleManipulator;

            internal Transform lastSelectionRayOrigin { get; private set; }

            public List<GameObject> ignoreList { private get; set; }

            public Rays()
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

            internal override void OnDestroy()
            {
                foreach (var proxy in m_Proxies)
                    ObjectUtils.Destroy(((MonoBehaviour)proxy).gameObject);
            }

            public void ConnectInterface(object target, object userData = null)
            {
                var rayOrigin = userData as Transform;
                if (rayOrigin)
                {
                    var evrDeviceData = evr.m_DeviceData;

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

            internal static void UpdateRayForDevice(DeviceData deviceData, Transform rayOrigin)
            {
                var mainMenu = deviceData.mainMenu;
                var customMenu = deviceData.customMenu;

                if (mainMenu != null)
                {
                    if (mainMenu.menuHideFlags == 0 || customMenu != null && customMenu.menuHideFlags == 0)
                        AddVisibilitySettings(rayOrigin, mainMenu, false, false);
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
                if (hoveredObject && hoveredObject.CompareTag(k_VRPlayerTag))
                {
                    // Clear the selection so that we do not manipulate it when moving the player head
                    Selection.activeObject = null;
                    return true;
                }

                return false;
            }

            internal void CreateAllProxies()
            {
                var deviceInputModule = evr.GetModule<DeviceInputModule>();
                var cameraRig = CameraUtils.GetCameraRig();
                foreach (var proxyType in ObjectUtils.GetImplementationsOfInterface(typeof(IProxy)))
                {
                    var proxy = (IProxy)ObjectUtils.CreateGameObjectWithComponent(proxyType, cameraRig, false);
                    this.ConnectInterfaces(proxy);
                    proxy.trackedObjectInput = deviceInputModule.trackedObjectInput;
                    proxy.activeChanged += () => OnProxyActiveChanged(proxy);

                    m_Proxies.Add(proxy);
                }
            }

            void OnProxyActiveChanged(IProxy proxy)
            {
                proxy.hidden = !proxy.active;

                if (proxy.active)
                {
                    var evrDeviceData = evr.m_DeviceData;
                    if (!evrDeviceData.Any(dd => dd.proxy == proxy))
                    {
                        var inputModule = evr.GetModule<MultipleRayInputModule>();
                        var deviceInputModule = evr.GetModule<DeviceInputModule>();
                        var keyboardModule = evr.GetModule<KeyboardModule>();
                        var highlightModule = evr.GetModule<HighlightModule>();
                        var workspaceModule = evr.GetModule<WorkspaceModule>();
                        var intersectionModule = evr.GetModule<IntersectionModule>();
                        var ui = evr.GetNestedModule<UI>();

                        foreach (var rayOriginPair in proxy.rayOrigins)
                        {
                            var node = rayOriginPair.Key;
                            var rayOrigin = rayOriginPair.Value;

                            var systemDevices = deviceInputModule.GetSystemDevices();
                            for (int j = 0; j < systemDevices.Count; j++)
                            {
                                var device = systemDevices[j];

                                // Find device tagged with the node that matches this RayOrigin node
                                var deviceNode = deviceInputModule.GetDeviceNode(device);
                                if (deviceNode == node)
                                {
                                    var deviceData = new DeviceData();
                                    evrDeviceData.Add(deviceData);
                                    deviceData.proxy = proxy;
                                    deviceData.node = node;
                                    deviceData.rayOrigin = rayOrigin;
                                    deviceData.inputDevice = device;

                                    // Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
                                    inputModule.AddRaycastSource(proxy, node, rayOrigin, source =>
                                    {
                                        // Do not invalidate UI raycasts in the middle of a drag operation
                                        if (!source.draggedObject)
                                        {
                                            var sourceRayOrigin = source.rayOrigin;
                                            if (evr.GetNestedModule<DirectSelection>().IsHovering(sourceRayOrigin))
                                                return false;

                                            var hoveredObject = source.hoveredObject;

                                            // The manipulator needs rays to go through scene objects in order to work
                                            var isManipulator = hoveredObject && hoveredObject.GetComponentInParent<IManipulator>() != null;
                                            float sceneObjectDistance;
                                            var raycastObject = intersectionModule.GetFirstGameObject(sourceRayOrigin, out sceneObjectDistance);
                                            var uiDistance = source.eventData.pointerCurrentRaycast.distance;

                                            // If the distance to a scene object is less than the distance to the hovered UI, invalidate the UI raycast
                                            if (!isManipulator && raycastObject && sceneObjectDistance < uiDistance && !ignoreList.Contains(raycastObject))
                                                return false;
                                        }

                                        if (!Menus.IsValidHover(source))
                                            return false;

                                        // Proceed only for raycast sources that haven't been blocked via IBlockUIInteraction
                                        if (source.blocked)
                                            return false;

                                        return true;
                                    });
                                }
                            }

                            rayOrigin.name = string.Format("{0} Ray Origin", node);
                            var rayTransform = ObjectUtils.Instantiate(evr.m_ProxyRayPrefab.gameObject, rayOrigin).transform;
                            rayTransform.position = rayOrigin.position;
                            rayTransform.rotation = rayOrigin.rotation;
                            var dpr = rayTransform.GetComponent<DefaultProxyRay>();
                            dpr.SetColor(highlightModule.highlightColor);
                            m_DefaultRays.Add(rayOrigin, dpr);

                            keyboardModule.SpawnKeyboardMallet(rayOrigin);

                            var proxyExtras = evr.m_ProxyExtras;
                            if (proxyExtras)
                            {
                                var extraData = proxyExtras.data;
                                List<GameObject> prefabs;
                                if (extraData.TryGetValue(rayOriginPair.Key, out prefabs))
                                {
                                    foreach (var prefab in prefabs)
                                    {
                                        var go = ui.InstantiateUI(prefab);
                                        go.transform.SetParent(rayOriginPair.Value, false);
                                    }
                                }
                            }

                            var tester = rayOriginPair.Value.GetComponentInChildren<IntersectionTester>();
                            tester.active = proxy.active;
                            intersectionModule.AddTester(tester);

                            highlightModule.AddRayOriginForNode(node, rayOrigin);

                            switch (node)
                            {
                                case Node.LeftHand:
                                    workspaceModule.leftRayOrigin = rayOrigin;
                                    break;
                                case Node.RightHand:
                                    workspaceModule.rightRayOrigin = rayOrigin;
                                    break;
                            }
                        }

                        evr.GetNestedModule<Tools>().SpawnDefaultTools(proxy);
                    }
                }
            }

            internal static void UpdateRaycasts()
            {
                var intersectionModule = evr.GetModule<IntersectionModule>();
                var distance = k_DefaultRayLength * Viewer.GetViewerScale();
                foreach (var deviceData in evr.m_DeviceData)
                {
                    var proxy = deviceData.proxy;
                    if (!proxy.active)
                        continue;

                    intersectionModule.UpdateRaycast(deviceData.rayOrigin, distance);
                }
            }

            internal void UpdateDefaultProxyRays()
            {
                var intersectionModule = evr.GetModule<IntersectionModule>();
                var inputModule = evr.GetModule<MultipleRayInputModule>();

                // Set ray lengths based on renderer bounds
                foreach (var proxy in m_Proxies)
                {
                    if (!proxy.active)
                        continue;

                    foreach (var kvp in proxy.rayOrigins)
                    {
                        var rayOrigin = kvp.Value;
                        var distance = k_DefaultRayLength * Viewer.GetViewerScale();

                        // Give UI priority over scene objects (e.g. For the TransformTool, handles are generally inside of the
                        // object, so visually show the ray terminating there instead of the object; UI is already given
                        // priority on the input side)
                        var uiEventData = inputModule.GetPointerEventData(rayOrigin);
                        if (uiEventData != null && uiEventData.pointerCurrentRaycast.isValid)
                        {
                            // Set ray length to distance to UI objects
                            distance = uiEventData.pointerCurrentRaycast.distance;
                        }
                        else
                        {
                            float hitDistance;
                            if (intersectionModule.GetFirstGameObject(rayOrigin, out hitDistance))
                                distance = hitDistance;
                        }

                        m_DefaultRays[rayOrigin].SetLength(distance);
                    }
                }
            }

            internal static void ForEachProxyDevice(ForEachProxyDeviceCallback callback, bool activeOnly = true)
            {
                var evrDeviceData = evr.m_DeviceData;
                for (var i = 0; i < evrDeviceData.Count; i++)
                {
                    var deviceData = evrDeviceData[i];
                    var proxy = deviceData.proxy;
                    if (activeOnly && !proxy.active)
                        continue;

                    callback(deviceData);
                }
            }

            static void IterateRayOrigins(ForEachRayOriginCallback callback)
            {
                var evrDeviceData = evr.m_DeviceData;
                for (var i = 0; i < evrDeviceData.Count; i++)
                {
                    var deviceData = evrDeviceData[i];
                    var proxy = deviceData.proxy;
                    if (!proxy.active)
                        continue;

                    callback(deviceData.rayOrigin);
                }
            }

            internal static IProxy GetProxyForRayOrigin(Transform rayOrigin)
            {
                IProxy result = null;
                var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
                if (deviceData != null)
                    result = deviceData.proxy;

                return result;
            }

            static GameObject GetFirstGameObject(Transform rayOrigin)
            {
                var intersectionModule = evr.GetModule<IntersectionModule>();

                float distance;
                var go = intersectionModule.GetFirstGameObject(rayOrigin, out distance);
                if (go)
                    return go;

                // If a raycast did not find an object use the spatial hash as a final test
                var renderer = intersectionModule.GetIntersectedObjectForRayOrigin(rayOrigin);
                if (renderer && !renderer.CompareTag(k_VRPlayerTag))
                    return renderer.gameObject;

                foreach (var kvp in evr.GetNestedModule<MiniWorlds>().rays)
                {
                    var miniWorldRay = kvp.Value;
                    if (miniWorldRay.originalRayOrigin.Equals(rayOrigin))
                    {
                        renderer = intersectionModule.GetIntersectedObjectForRayOrigin(kvp.Key);
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

            internal static void AddVisibilitySettings(Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0)
            {
                if (rayOrigin)
                {
                    var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                    if (dpr)
                        dpr.AddVisibilitySettings(caller, rayVisible, coneVisible, priority);
                }
            }

            internal static void RemoveVisibilitySettings(Transform rayOrigin, object obj)
            {
                if (!rayOrigin) // Prevent MissingReferenceException on closing EVR
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

                if (!m_StandardManipulator)
                {
                    m_StandardManipulator = evr.GetComponentInChildren<StandardManipulator>();
                    if (m_StandardManipulator)
                        ConnectInterface(m_StandardManipulator);
                }

                if (m_StandardManipulator)
                    m_StandardManipulator.AdjustScale(cameraPosition, matrix);

                if (!m_ScaleManipulator)
                    m_ScaleManipulator = evr.GetComponentInChildren<ScaleManipulator>();

                if (m_ScaleManipulator)
                    m_ScaleManipulator.AdjustScale(cameraPosition, matrix);
            }

            static Node RequestNodeFromRayOrigin(Transform rayOrigin)
            {
                if (rayOrigin == null)
                    return Node.None;

                foreach (var deviceData in evr.m_DeviceData)
                {
                    if (!deviceData.proxy.active)
                        continue;

                    if (deviceData.rayOrigin == rayOrigin)
                        return deviceData.node;
                }

                foreach (var kvp in evr.GetNestedModule<MiniWorlds>().rays)
                {
                    if (kvp.Key == rayOrigin)
                        return kvp.Value.node;
                }

                return Node.None;
            }

            static Transform RequestRayOriginFromNode(Node node)
            {
                if (node == Node.None)
                    return null;

                foreach (var deviceData in evr.m_DeviceData)
                {
                    if (!deviceData.proxy.active)
                        continue;

                    if (deviceData.node == node)
                        return deviceData.rayOrigin;
                }

                foreach (var kvp in evr.GetNestedModule<MiniWorlds>().rays)
                {
                    if (kvp.Value.node == node)
                        return kvp.Value.originalRayOrigin;
                }

                return null;
            }

            static void SetDefaultRayColor(Transform rayOrigin, Color color)
            {
                if (rayOrigin)
                {
                    var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                    if (dpr)
                    {
                        dpr.SetColor(color);
                    }
                }

                var highlightModule = evr.GetModule<HighlightModule>();
                highlightModule.highlightColor = color;
            }

            static Color GetDefaultRayColor(Transform rayOrigin)
            {
                if (rayOrigin)
                {
                    var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
                    if (dpr)
                    {
                        return dpr.GetColor();
                    }
                }

                var highlightModule = evr.GetModule<HighlightModule>();
                return highlightModule.highlightColor;
            }
        }
    }
}

