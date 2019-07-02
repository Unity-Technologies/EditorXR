#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    class MenuHideData
    {
        public MenuHideFlags hideFlags = MenuHideFlags.Hidden;
        public MenuHideFlags lastHideFlags = MenuHideFlags.Hidden;
        public float autoHideTime;
        public float autoShowTime;
    }

    [ModuleOrder(ModuleOrders.MenuModuleLoadOrder)]
    [ModuleBehaviorCallbackOrder(ModuleOrders.MenuModuleBehaviorOrder)]
    class EditorXRMenuModule : MonoBehaviour, IModuleDependency<EditorVR>, IModuleDependency<EditorXRToolModule>,
        IModuleDependency<EditorXRRayModule>, IModuleDependency<EditorXRViewerModule>,
        IModuleDependency<DeviceInputModule>, IModuleDependency<EditorXRDirectSelectionModule>,
        IModuleDependency<EditorXRUIModule>, IInterfaceConnector, IConnectInterfaces, IInitializableModule,
        IModuleBehaviorCallbacks
    {
        const float k_MainMenuAutoHideDelay = 0.125f;
        const float k_MainMenuAutoShowDelay = 0.25f;

        const float k_MenuHideMargin = 0.075f;
        const float k_TwoHandHideDistance = 0.25f;
        const int k_PossibleOverlaps = 16;

        readonly Dictionary<Transform, IMainMenu> m_MainMenus = new Dictionary<Transform, IMainMenu>();
        readonly Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuProvider> m_SettingsMenuProviders = new Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuProvider>();
        readonly Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuItemProvider> m_SettingsMenuItemProviders = new Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuItemProvider>();
        List<Type> m_MainMenuTools;

        EditorVR m_EditorVR;
        EditorXRRayModule m_RayModule;
        EditorXRViewerModule m_ViewerModule;
        DeviceInputModule m_DeviceInputModule;
        EditorXRDirectSelectionModule m_DirectSelectionModule;
        EditorXRUIModule m_UIModule;
        EditorXRToolModule m_ToolModule;

        public int initializationOrder { get { return 1; } }
        public int shutdownOrder { get { return 0; } }
        public int connectInterfaceOrder { get { return 0; } }

        // Local method use only -- created here to reduce garbage collection
        static readonly List<DeviceData> k_ActiveDeviceData = new List<DeviceData>();
        static readonly List<IWorkspace> k_WorkspaceComponents = new List<IWorkspace>();
        static readonly List<GradientButton> k_ButtonComponents = new List<GradientButton>();
        static readonly Collider[] k_ColliderOverlaps = new Collider[k_PossibleOverlaps];

        public void ConnectDependency(EditorVR dependency)
        {
            m_EditorVR = dependency;
        }

        public void ConnectDependency(EditorXRToolModule dependency)
        {
            m_ToolModule = dependency;
        }

        public void ConnectDependency(EditorXRRayModule dependency)
        {
            m_RayModule = dependency;
        }

        public void ConnectDependency(EditorXRViewerModule dependency)
        {
            m_ViewerModule = dependency;
        }

        public void ConnectDependency(DeviceInputModule dependency)
        {
            m_DeviceInputModule = dependency;
        }

        public void ConnectDependency(EditorXRDirectSelectionModule dependency)
        {
            m_DirectSelectionModule = dependency;
        }

        public void ConnectDependency(EditorXRUIModule dependency)
        {
            m_UIModule = dependency;
        }

        public void LoadModule()
        {
            IInstantiateMenuUIMethods.instantiateMenuUI = InstantiateMenuUI;
            IIsMainMenuVisibleMethods.isMainMenuVisible = IsMainMenuVisible;
            IUsesCustomMenuOriginsMethods.getCustomMenuOrigin = GetCustomMenuOrigin;
            IUsesCustomMenuOriginsMethods.getCustomAlternateMenuOrigin = GetCustomAlternateMenuOrigin;
        }

        public void UnloadModule() { }

        public void Initialize()
        {
            m_MainMenuTools = m_ToolModule.allTools.Where(t =>
            {
                return !EditorXRToolModule.IsDefaultTool(t) && !EditorVR.HiddenTypes.Contains(t);
            }).ToList(); // Don't show tools that can't be selected/toggled
        }

        public void Shutdown() { }

        public void ConnectInterface(object target, object userData = null)
        {
            var rayOrigin = userData as Transform;
            var settingsMenuProvider = target as ISettingsMenuProvider;
            if (settingsMenuProvider != null)
            {
                m_SettingsMenuProviders[new KeyValuePair<Type, Transform>(target.GetType(), rayOrigin)] = settingsMenuProvider;
                foreach (var kvp in m_MainMenus)
                {
                    if (rayOrigin == null || kvp.Key == rayOrigin)
                        kvp.Value.AddSettingsMenu(settingsMenuProvider);
                }
            }

            var settingsMenuItemProvider = target as ISettingsMenuItemProvider;
            if (settingsMenuItemProvider != null)
            {
                m_SettingsMenuItemProviders[new KeyValuePair<Type, Transform>(target.GetType(), rayOrigin)] = settingsMenuItemProvider;
                foreach (var kvp in m_MainMenus)
                {
                    if (rayOrigin == null || kvp.Key == rayOrigin)
                        kvp.Value.AddSettingsMenuItem(settingsMenuItemProvider);
                }
            }

            var mainMenu = target as IMainMenu;
            if (mainMenu != null && rayOrigin != null)
            {
                mainMenu.menuTools = m_MainMenuTools;
                mainMenu.menuWorkspaces = WorkspaceModule.workspaceTypes.Where(t => !EditorVR.HiddenTypes.Contains(t)).ToList();
                mainMenu.settingsMenuProviders = m_SettingsMenuProviders;
                mainMenu.settingsMenuItemProviders = m_SettingsMenuItemProviders;
                m_MainMenus[rayOrigin] = mainMenu;
            }

            var menuOrigins = target as IUsesMenuOrigins;
            if (menuOrigins != null)
            {
                Transform mainMenuOrigin;
                var proxy = m_RayModule.GetProxyForRayOrigin(rayOrigin);
                if (proxy != null && proxy.menuOrigins.TryGetValue(rayOrigin, out mainMenuOrigin))
                {
                    menuOrigins.menuOrigin = mainMenuOrigin;
                    Transform alternateMenuOrigin;
                    if (proxy.alternateMenuOrigins.TryGetValue(rayOrigin, out alternateMenuOrigin))
                        menuOrigins.alternateMenuOrigin = alternateMenuOrigin;
                }
            }

            var alternateMenu = target as IAlternateMenu;
            if (alternateMenu != null)
                AddAlternateMenu(alternateMenu, rayOrigin);

            var spatialMenuProvider = target as ISpatialMenuProvider;
            if (spatialMenuProvider != null)
                SpatialMenu.AddProvider(spatialMenuProvider);
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var rayOrigin = userData as Transform;
            var settingsMenuProvider = target as ISettingsMenuProvider;
            if (settingsMenuProvider != null)
            {
                foreach (var kvp in m_MainMenus)
                {
                    if (rayOrigin == null || kvp.Key == rayOrigin)
                        kvp.Value.RemoveSettingsMenu(settingsMenuProvider);
                }

                m_SettingsMenuProviders.Remove(new KeyValuePair<Type, Transform>(target.GetType(), rayOrigin));
            }

            var settingsMenuItemProvider = target as ISettingsMenuItemProvider;
            if (settingsMenuItemProvider != null)
            {
                foreach (var kvp in m_MainMenus)
                {
                    if (rayOrigin == null || kvp.Key == rayOrigin)
                        kvp.Value.RemoveSettingsMenuItem(settingsMenuItemProvider);
                }

                m_SettingsMenuItemProviders.Remove(new KeyValuePair<Type, Transform>(target.GetType(), rayOrigin));
            }

            var mainMenu = target as IMainMenu;
            if (mainMenu != null && rayOrigin != null)
                m_MainMenus.Remove(rayOrigin);

            var alternateMenu = target as IAlternateMenu;
            if (alternateMenu != null)
                RemoveAlternateMenu(alternateMenu);
        }

        void AddAlternateMenu(IAlternateMenu alternateMenu, Transform rayOrigin)
        {
            foreach (var device in m_EditorVR.deviceData)
            {
                if (device.rayOrigin != rayOrigin)
                    continue;

                device.alternateMenus.Add(alternateMenu);
                var menuHideData = new MenuHideData();
                device.menuHideData[alternateMenu] = menuHideData;

                // Alternate menus must be visible the first frame or they are ignored in the priority list
                menuHideData.hideFlags = 0;

                break;
            }
        }

        void RemoveAlternateMenu(IAlternateMenu alternateMenu)
        {
            foreach (var device in m_EditorVR.deviceData)
            {
                device.alternateMenus.Remove(alternateMenu);
                device.menuHideData.Remove(alternateMenu);
            }
        }

        static void UpdateAlternateMenuForDevice(DeviceData deviceData)
        {
            var alternateMenu = deviceData.alternateMenu;
            if (alternateMenu == null || !deviceData.menuHideData.ContainsKey(alternateMenu))
                return;

            alternateMenu.menuHideFlags = deviceData.currentTool is IExclusiveMode ? 0 : deviceData.menuHideData[alternateMenu].hideFlags;

            // Move the Tools Menu buttons to an alternate position if the radial menu will be shown
            deviceData.toolsMenu.alternateMenuVisible = alternateMenu.menuHideFlags == 0 && alternateMenu is RadialMenu;
        }

        Transform GetCustomMenuOrigin(Transform rayOrigin)
        {
            Transform mainMenuOrigin = null;

            var proxy = m_RayModule.GetProxyForRayOrigin(rayOrigin);
            if (proxy != null)
            {
                var menuOrigins = proxy.menuOrigins;
                if (menuOrigins.ContainsKey(rayOrigin))
                    mainMenuOrigin = menuOrigins[rayOrigin];
            }

            return mainMenuOrigin;
        }

        Transform GetCustomAlternateMenuOrigin(Transform rayOrigin)
        {
            Transform alternateMenuOrigin = null;

            var proxy = m_RayModule.GetProxyForRayOrigin(rayOrigin);
            if (proxy != null)
            {
                var alternateMenuOrigins = proxy.alternateMenuOrigins;
                if (alternateMenuOrigins.ContainsKey(rayOrigin))
                    alternateMenuOrigin = alternateMenuOrigins[rayOrigin];
            }

            return alternateMenuOrigin;
        }

        internal void UpdateMenuVisibilities()
        {
            k_ActiveDeviceData.Clear();
            m_RayModule.ForEachProxyDevice(deviceData => { k_ActiveDeviceData.Add(deviceData); });

            foreach (var deviceData in k_ActiveDeviceData)
            {
                IAlternateMenu alternateMenu = null;
                var menuHideData = deviceData.menuHideData;

                // Always display the highest-priority alternate menu, and hide all others.
                var alternateMenus = deviceData.alternateMenus;
                foreach (var menu in alternateMenus)
                {
                    var hideData = menuHideData[menu];
                    if ((hideData.hideFlags & MenuHideFlags.Hidden) == 0
                        && (alternateMenu == null || menu.priority >= alternateMenu.priority))
                        alternateMenu = menu;

                    hideData.hideFlags |= MenuHideFlags.OtherMenu;
                }

                deviceData.alternateMenu = alternateMenu;
                menuHideData[alternateMenu].hideFlags = 0;
                var mainMenu = deviceData.mainMenu;
                var customMenu = deviceData.customMenu;
                MenuHideData customMenuHideData = null;

                var mainMenuVisible = mainMenu != null && menuHideData[mainMenu].hideFlags == 0;
                var mainMenuSuppressed = mainMenu != null && ((menuHideData[mainMenu].hideFlags & MenuHideFlags.Occluded) != 0);

                var alternateMenuData = menuHideData[alternateMenu];
                var alternateMenuVisible = alternateMenuData.hideFlags == 0;

                var customMenuVisible = false;
                if (customMenu != null)
                {
                    customMenuHideData = menuHideData[customMenu];
                    customMenuVisible = customMenuHideData.hideFlags == 0;
                }

                // Temporarily hide customMenu if other menus are visible or should be
                if (customMenuVisible && (mainMenuVisible || mainMenuSuppressed))
                    customMenuHideData.hideFlags |= MenuHideFlags.OtherMenu;

                // Kick the alternate menu to the other hand if a main menu or custom menu is visible
                if (alternateMenuVisible && (mainMenuVisible || customMenuVisible) && alternateMenu is RadialMenu)
                {
                    foreach (var otherDeviceData in k_ActiveDeviceData)
                    {
                        if (otherDeviceData == deviceData)
                            continue;

                        SetAlternateMenuVisibility(otherDeviceData.rayOrigin, true);
                        break;
                    }
                }

                // Check if menu bounds overlap with any workspace colliders
                foreach (var kvp in menuHideData)
                {
                    CheckMenuColliderOverlaps(kvp.Key, kvp.Value);
                }

                // Check if there are currently any held objects, or if the other hand is in proximity for scaling
                CheckDirectSelection(deviceData, menuHideData, alternateMenuVisible);
            }

            // Set show/hide timings
            foreach (var deviceData in k_ActiveDeviceData)
            {
                foreach (var kvp in deviceData.menuHideData)
                {
                    var hideFlags = kvp.Value.hideFlags;
                    if ((hideFlags & ~MenuHideFlags.Hidden & ~MenuHideFlags.OtherMenu) == 0)
                        kvp.Value.autoHideTime = Time.time;

                    if (hideFlags != 0)
                    {
                        var menuHideData = kvp.Value;
                        menuHideData.lastHideFlags = menuHideData.hideFlags;
                        kvp.Value.autoShowTime = Time.time;
                    }
                }
            }

            // Apply MenuHideFlags to UI visibility
            foreach (var deviceData in k_ActiveDeviceData)
            {
                var mainMenu = deviceData.mainMenu;
                if (mainMenu != null)
                {
                    var mainMenuHideData = deviceData.menuHideData[mainMenu];
                    var mainMenuHideFlags = mainMenuHideData.hideFlags;
                    var lastMainMenuHideFlags = mainMenuHideData.lastHideFlags;

                    var permanentlyHidden = (mainMenuHideFlags & MenuHideFlags.Hidden) != 0;
                    var wasPermanentlyHidden = (lastMainMenuHideFlags & MenuHideFlags.Hidden) != 0;

                    //Temporary states take effect after a delay
                    var temporarilyHidden = (mainMenuHideFlags & MenuHideFlags.Temporary) != 0
                        && Time.time > mainMenuHideData.autoHideTime + k_MainMenuAutoHideDelay;
                    var wasTemporarilyHidden = (lastMainMenuHideFlags & MenuHideFlags.Temporary) != 0
                        && Time.time > mainMenuHideData.autoShowTime + k_MainMenuAutoShowDelay;

                    // If the menu is focused, only hide if Hidden is set (e.g. not temporary) in order to hide the selected tool
                    if (permanentlyHidden || wasPermanentlyHidden || !mainMenu.focus && (temporarilyHidden || wasTemporarilyHidden))
                        mainMenu.menuHideFlags = mainMenuHideFlags;

                    // Disable the main menu activator if any temporary states are set
                    deviceData.toolsMenu.mainMenuActivatorInteractable = (mainMenuHideFlags & MenuHideFlags.Temporary) == 0
                        && mainMenu.menuContent;
                }

                // Show/hide custom menu, if it exists
                var customMenu = deviceData.customMenu;
                if (customMenu != null)
                    customMenu.menuHideFlags = deviceData.menuHideData[customMenu].hideFlags;

                var alternateMenus = deviceData.alternateMenus;
                foreach (var menu in alternateMenus)
                {
                    menu.menuHideFlags = deviceData.menuHideData[menu].hideFlags;
                }

                UpdateAlternateMenuForDevice(deviceData);
                m_RayModule.UpdateRayForDevice(deviceData, deviceData.rayOrigin);
            }

            foreach (var deviceData in k_ActiveDeviceData)
            {
                foreach (var kvp in deviceData.menuHideData)
                {
                    kvp.Value.hideFlags &= ~MenuHideFlags.Temporary;
                }
            }

            m_DeviceInputModule.UpdatePlayerHandleMaps();
        }
        void CheckDirectSelection(DeviceData deviceData, Dictionary<IMenu, MenuHideData> menuHideData, bool alternateMenuVisible)
        {
            var viewerScale = m_ViewerModule.GetViewerScale();
            var rayOrigin = deviceData.rayOrigin;
            var rayOriginPosition = rayOrigin.position;
            var heldObjects = m_DirectSelectionModule.GetHeldObjects(rayOrigin);

            // If this hand is holding any objects, hide its menus
            var hasDirectSelection = heldObjects != null && heldObjects.Count > 0;
            if (hasDirectSelection)
            {
                foreach (var kvp in menuHideData)
                {
                    // Only set if hidden--value is reset every frame
                    kvp.Value.hideFlags |= MenuHideFlags.HasDirectSelection;
                }

                foreach (var otherDeviceData in k_ActiveDeviceData)
                {
                    if (otherDeviceData == deviceData)
                        continue;

                    var otherRayOrigin = otherDeviceData.rayOrigin;
                    if (alternateMenuVisible)
                        SetAlternateMenuVisibility(otherRayOrigin, true);

                    // If other hand is within range to do a two-handed scale, hide its menu as well
                    if (m_DirectSelectionModule.IsHovering(otherRayOrigin) || m_DirectSelectionModule.IsScaling(otherRayOrigin)
                        || Vector3.Distance(otherRayOrigin.position, rayOriginPosition) < k_TwoHandHideDistance * viewerScale)
                    {
                        foreach (var kvp in otherDeviceData.menuHideData)
                        {
                            // Only set if hidden--value is reset every frame
                            kvp.Value.hideFlags |= MenuHideFlags.HasDirectSelection;
                        }

                        break;
                    }
                }
            }
        }

        void CheckMenuColliderOverlaps(IMenu menu, MenuHideData menuHideData)
        {
            var menuBounds = menu.localBounds;
            if (menuBounds.extents == Vector3.zero)
                return;

            Array.Clear(k_ColliderOverlaps, 0, k_ColliderOverlaps.Length);
            var hoveringCollider = false;
            var menuTransform = menu.menuContent.transform;
            var menuRotation = menuTransform.rotation;
            var viewerScale = m_ViewerModule.GetViewerScale();
            var center = menuTransform.position + menuRotation * menuBounds.center * viewerScale;
            if (Physics.OverlapBoxNonAlloc(center, menuBounds.extents * viewerScale, k_ColliderOverlaps, menuRotation) > 0)
            {
                foreach (var overlap in k_ColliderOverlaps)
                {
                    if (overlap)
                    {
                        k_WorkspaceComponents.Clear();
                        overlap.GetComponents(k_WorkspaceComponents);
                        if (k_WorkspaceComponents.Count > 0)
                            hoveringCollider = true;

                        if (menu is MainMenu)
                        {
                            k_ButtonComponents.Clear();
                            overlap.GetComponents(k_ButtonComponents);
                            if (k_ButtonComponents.Count > 0)
                                hoveringCollider = true;
                        }
                    }
                }
            }

            // Only set if hidden--value is reset every frame
            if (hoveringCollider)
                menuHideData.hideFlags |= MenuHideFlags.OverWorkspace;
        }

        internal bool IsValidHover(MultipleRayInputModule.RaycastSource source)
        {
            var go = source.draggedObject;
            if (!go)
                go = source.hoveredObject;

            if (!go)
                return true;

            if (go == gameObject)
                return true;

            var eventData = source.eventData;
            var rayOrigin = eventData.rayOrigin;

            DeviceData deviceData = null;
            foreach (var currentDevice in m_EditorVR.deviceData)
            {
                if (currentDevice.rayOrigin == rayOrigin)
                {
                    deviceData = currentDevice;
                    break;
                }
            }

            if (deviceData != null)
            {
                if (go.transform.IsChildOf(deviceData.rayOrigin)) // Don't let UI on this hand block the menu
                    return false;

                var scaledPointerDistance = eventData.pointerCurrentRaycast.distance / m_ViewerModule.GetViewerScale();
                var menuHideFlags = deviceData.menuHideData;
                var mainMenu = deviceData.mainMenu;
                IMenu openMenu = mainMenu;
                if (deviceData.customMenu != null && menuHideFlags[mainMenu].hideFlags != 0)
                    openMenu = deviceData.customMenu;

                if (openMenu == null)
                    return false;

                if (scaledPointerDistance < openMenu.localBounds.size.y + k_MenuHideMargin)
                {
                    // Only set if hidden--value is reset every frame
                    menuHideFlags[openMenu].hideFlags |= MenuHideFlags.OverUI;
                    return true;
                }

                return menuHideFlags[openMenu].hideFlags != 0;
            }

            return true;
        }

        internal void UpdateAlternateMenuOnSelectionChanged(Transform rayOrigin)
        {
            if (rayOrigin == null)
                return;

            SetAlternateMenuVisibility(rayOrigin, Selection.gameObjects.Length > 0);
        }

        internal void SetAlternateMenuVisibility(Transform rayOrigin, bool visible)
        {
            m_RayModule.ForEachProxyDevice(deviceData =>
            {
                foreach (var menu in deviceData.alternateMenus)
                {
                    if (!(menu is IActionsMenu))
                        continue;

                    var menuHideFlags = deviceData.menuHideData;

                    // Set alternate menu visible on this rayOrigin and hide it on all others
                    var alternateMenuData = menuHideFlags[menu];
                    if (deviceData.rayOrigin == rayOrigin && visible)
                        alternateMenuData.hideFlags &= ~MenuHideFlags.Hidden;
                    else
                        alternateMenuData.hideFlags |= MenuHideFlags.Hidden;
                }
            });
        }

        internal void OnMainMenuActivatorSelected(Transform rayOrigin, Transform targetRayOrigin)
        {
            foreach (var deviceData in m_EditorVR.deviceData)
            {
                var mainMenu = deviceData.mainMenu;
                if (mainMenu != null)
                {
                    var customMenu = deviceData.customMenu;
                    var alternateMenu = deviceData.alternateMenu;
                    var menuHideData = deviceData.menuHideData;
                    var mainMenuHideData = menuHideData[mainMenu];
                    var alternateMenuVisible = alternateMenu != null
                        && (menuHideData[alternateMenu].hideFlags & MenuHideFlags.Hidden) == 0;

                    // Do not delay when showing via activator
                    mainMenuHideData.autoShowTime = 0;

                    if (deviceData.rayOrigin == rayOrigin)
                    {
                        // Toggle main menu hidden flag
                        if (mainMenu.menuContent)
                            mainMenuHideData.hideFlags ^= MenuHideFlags.Hidden;

                        mainMenu.targetRayOrigin = targetRayOrigin;
                    }
                    else
                    {
                        if (mainMenu.menuContent)
                            mainMenuHideData.hideFlags |= MenuHideFlags.Hidden;

                        var customMenuOverridden = customMenu != null &&
                            (menuHideData[customMenu].hideFlags & MenuHideFlags.OtherMenu) != 0;

                        // Move alternate menu if overriding custom menu
                        if (customMenuOverridden && alternateMenuVisible)
                        {
                            foreach (var otherDeviceData in m_EditorVR.deviceData)
                            {
                                if (deviceData == otherDeviceData)
                                    continue;

                                SetAlternateMenuVisibility(rayOrigin, true);
                            }
                        }
                    }
                }
            }
        }

        GameObject InstantiateMenuUI(Transform rayOrigin, IMenu prefab)
        {
            GameObject go = null;
            m_RayModule.ForEachProxyDevice(deviceData =>
            {
                var proxy = deviceData.proxy;
                var otherRayOrigin = deviceData.rayOrigin;
                if (proxy.rayOrigins.ContainsValue(rayOrigin) && otherRayOrigin != rayOrigin)
                {
                    Transform menuOrigin;
                    if (proxy.menuOrigins.TryGetValue(otherRayOrigin, out menuOrigin))
                    {
                        if (deviceData.customMenu == null)
                        {
                            go = m_UIModule.InstantiateUI(prefab.gameObject, menuOrigin, false);

                            var customMenu = go.GetComponent<IMenu>();
                            deviceData.customMenu = customMenu;
                            deviceData.menuHideData[customMenu] = new MenuHideData { hideFlags = 0 };
                        }
                    }
                }
            });

            return go;
        }

        internal T SpawnMenu<T>(Transform rayOrigin) where T : MonoBehaviour, IMenu
        {
            return (T)SpawnMenu(typeof(T), rayOrigin);
        }

        internal IMenu SpawnMenu(Type menuType, Transform rayOrigin)
        {
            var spawnedMenu = (IMenu)EditorXRUtils.AddComponent(menuType, gameObject);
            this.ConnectInterfaces(spawnedMenu, rayOrigin);

            return spawnedMenu;
        }

        bool IsMainMenuVisible(Transform rayOrigin)
        {
            foreach (var deviceData in m_EditorVR.deviceData)
            {
                if (deviceData.mainMenu != null && deviceData.rayOrigin == rayOrigin)
                    return (deviceData.menuHideData[deviceData.mainMenu].hideFlags & MenuHideFlags.Hidden) == 0;
            }

            return false;
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            UpdateMenuVisibilities();
        }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }
    }
}
#endif
