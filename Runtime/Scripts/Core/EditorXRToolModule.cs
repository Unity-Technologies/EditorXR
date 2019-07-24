#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
    public class ToolData
    {
        public ITool tool;
        public ActionMapInput input;
        public Sprite icon;
    }

    class DeviceData
    {
        public IProxy proxy;
        public InputDevice inputDevice;
        public Node node;
        public Transform rayOrigin;
        public readonly Stack<ToolData> toolData = new Stack<ToolData>();
        public IMainMenu mainMenu;
        public ITool currentTool;
        public IMenu customMenu;
        public IToolsMenu toolsMenu;
        public readonly List<IAlternateMenu> alternateMenus = new List<IAlternateMenu>();
        public IAlternateMenu alternateMenu;
        public SpatialMenu spatialMenu;
        public readonly Dictionary<IMenu, MenuHideData> menuHideData = new Dictionary<IMenu, MenuHideData>();
    }

    public class EditorXRToolModule : MonoBehaviour,
        IInterfaceConnector, IUsesConnectInterfaces, IDelayedInitializationModule,
        IUsesFunctionalityInjection, IProvidesSelectTool
    {
        static readonly List<Type> k_AllTools = new List<Type>();

        readonly Dictionary<Type, List<ILinkedObject>> m_LinkedObjects = new Dictionary<Type, List<ILinkedObject>>();
        EditorXRRayModule m_RayModule;

        List<Type> allTools { get { return k_AllTools; } }

        internal readonly List<DeviceData> deviceData = new List<DeviceData>();

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }
        public int connectInterfaceOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        static EditorXRToolModule()
        {
            typeof(ITool).GetImplementationsOfInterface(k_AllTools);
        }

        public void LoadModule()
        {
            ILinkedObjectMethods.isSharedUpdater = IsSharedUpdater;
            m_RayModule = ModuleLoaderCore.instance.GetModule<EditorXRRayModule>();
        }

        public void UnloadModule()
        {
            m_LinkedObjects.Clear();
        }

        public void Initialize()
        {
            deviceData.Clear();
            var menuModule = ModuleLoaderCore.instance.GetModule<EditorXRMenuModule>();
            if (menuModule != null)
            {
                menuModule.mainMenuTools = allTools.Where(t =>
                {
                    return !IsDefaultTool(t) && !EditorVR.HiddenTypes.Contains(t);
                }).ToList(); // Don't show tools that can't be selected/toggled
            }

            m_LinkedObjects.Clear();
        }

        public void Shutdown()
        {
            m_LinkedObjects.Clear();

            foreach (var device in deviceData)
            {
                var mainMenu = device.mainMenu;
                this.DisconnectInterfaces(mainMenu);
                var behavior = mainMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                var alternateMenu = device.alternateMenu;
                this.DisconnectInterfaces(alternateMenu);
                behavior = alternateMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                var toolsMenu = device.toolsMenu;
                this.DisconnectInterfaces(toolsMenu);
                behavior = toolsMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                var customMenu = device.customMenu;
                this.DisconnectInterfaces(customMenu);
                behavior = customMenu as MonoBehaviour;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                var spatialMenu = device.spatialMenu;
                this.DisconnectInterfaces(spatialMenu);
                behavior = spatialMenu;
                if (behavior)
                    UnityObjectUtils.Destroy(behavior);

                foreach (var menu in device.alternateMenus.ToList())
                {
                    this.DisconnectInterfaces(menu);
                    behavior = menu as MonoBehaviour;
                    if (behavior)
                        UnityObjectUtils.Destroy(behavior);
                }

                foreach (var toolData in device.toolData.ToList())
                {
                    var tool = toolData.tool;
                    this.DisconnectInterfaces(tool);
                    behavior = tool as MonoBehaviour;
                    if (behavior)
                        UnityObjectUtils.Destroy(behavior);
                }
            }

            deviceData.Clear();
        }

        public void ConnectInterface(object target, object userData = null)
        {
            var linkedObject = target as ILinkedObject;
            if (linkedObject != null)
            {
                var type = target.GetType();
                List<ILinkedObject> linkedObjectList;
                if (!m_LinkedObjects.TryGetValue(type, out linkedObjectList))
                {
                    linkedObjectList = new List<ILinkedObject>();
                    m_LinkedObjects[type] = linkedObjectList;
                }

                linkedObjectList.Add(linkedObject);
                linkedObject.linkedObjects = linkedObjectList;
            }
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var linkedObject = target as ILinkedObject;
            if (linkedObject != null)
            {
                // Delay removal of linked objects in case shutdown logic relies on them
                // Specifically, SerializePreferences in AnnotationTool calls IsSharedUpdater
                EditorApplication.delayCall += () =>
                {
                    var type = target.GetType();
                    List<ILinkedObject> linkedObjectList;
                    if (!m_LinkedObjects.TryGetValue(type, out linkedObjectList))
                        return;

                    linkedObjectList.Remove(linkedObject);
                    linkedObject.linkedObjects = null;

                    if (linkedObjectList.Count == 0)
                        m_LinkedObjects.Remove(type);
                };
            }
        }

        bool IsSharedUpdater(ILinkedObject linkedObject)
        {
            var type = linkedObject.GetType();
            List<ILinkedObject> list;
            if (m_LinkedObjects.TryGetValue(type, out list))
                return m_LinkedObjects[type].IndexOf(linkedObject) == 0;

            return false;
        }

        internal static bool IsDefaultTool(Type type)
        {
            var defaultTools = EditorVR.DefaultTools;
            if (defaultTools == null)
                return false;

            return defaultTools.Contains(type);
        }

        public void SpawnDefaultTools(IProxy proxy)
        {
            var defaultTools = EditorVR.DefaultTools;
            if (defaultTools == null)
                return;

            var moduleLoaderCore = ModuleLoaderCore.instance;
            var menuModule = moduleLoaderCore.GetModule<EditorXRMenuModule>();
            var lockModule = moduleLoaderCore.GetModule<LockModule>();
            var deviceInputModule = moduleLoaderCore.GetModule<DeviceInputModule>();
            var vacuumablesModule = moduleLoaderCore.GetModule<EditorXRVacuumableModule>();
            var vacuumables = vacuumablesModule != null ? vacuumablesModule.vacuumables : new List<IVacuumable>();

            foreach (var device in deviceData)
            {
                var inputDevice = device.inputDevice;
                ToolData selectionToolData = null;

                if (device.proxy != proxy)
                    continue;

                var rayOrigin = device.rayOrigin;
                foreach (var toolType in defaultTools)
                {
                    HashSet<InputDevice> devices;
                    var toolData = SpawnTool(toolType, out devices, inputDevice, rayOrigin, deviceInputModule);
                    AddToolToDeviceData(toolData, devices);

                    var tool = toolData.tool;
                    var selectionTool = tool as SelectionTool;
                    if (selectionTool)
                    {
                        selectionToolData = toolData;
                        if (lockModule != null)
                            selectionTool.hovered += lockModule.OnHovered;
                    }

                    var vacuumTool = tool as VacuumTool;
                    if (vacuumTool)
                    {
                        vacuumTool.defaultOffset = WorkspaceModule.DefaultWorkspaceOffset;
                        vacuumTool.defaultTilt = WorkspaceModule.DefaultWorkspaceTilt;
                        vacuumTool.vacuumables = vacuumables;
                    }
                }

                IMainMenu mainMenu;
                var menuHideData = device.menuHideData;
                if (EditorVR.DefaultMenu != null)
                {
                    mainMenu = (IMainMenu)menuModule.SpawnMenu(EditorVR.DefaultMenu, rayOrigin);
                    device.mainMenu = mainMenu;
                    menuHideData[mainMenu] = new MenuHideData();
                }

                if (EditorVR.DefaultAlternateMenu != null)
                {
                    var alternateMenu = (IAlternateMenu)menuModule.SpawnMenu(EditorVR.DefaultAlternateMenu, rayOrigin);
                    menuHideData[alternateMenu] = new MenuHideData();
                    var radialMenu = alternateMenu as RadialMenu;
                    if (radialMenu)
                        radialMenu.itemWasSelected += menuModule.UpdateAlternateMenuOnSelectionChanged;
                }

                var undoMenu = menuModule.SpawnMenu<UndoMenu>(rayOrigin);
                var hideData = new MenuHideData();
                menuHideData[undoMenu] = hideData;
                hideData.hideFlags = 0;

                // Setup ToolsMenu
                ToolsMenu toolsMenu = null;
                var toolsMenus = gameObject.GetComponents<ToolsMenu>();
                foreach (var m in toolsMenus)
                {
                    if (!m.enabled)
                    {
                        toolsMenu = m;
                        break;
                    }
                }

                if (!toolsMenu)
                    toolsMenu = EditorXRUtils.AddComponent<ToolsMenu>(gameObject);

                toolsMenu.enabled = true;
                this.ConnectInterfaces(toolsMenu, rayOrigin);
                this.InjectFunctionalitySingle(toolsMenu);
                device.toolsMenu = toolsMenu;
                toolsMenu.rayOrigin = rayOrigin;
                toolsMenu.setButtonForType(typeof(IMainMenu), null);
                toolsMenu.setButtonForType(typeof(SelectionTool), selectionToolData != null ? selectionToolData.icon : null);

                var spatialMenu = EditorXRUtils.AddComponent<SpatialMenu>(gameObject);
                this.ConnectInterfaces(spatialMenu, rayOrigin);
                this.InjectFunctionalitySingle(spatialMenu);
                spatialMenu.Setup();
                device.spatialMenu = spatialMenu;
            }

            if (deviceInputModule != null)
                deviceInputModule.UpdatePlayerHandleMaps();
        }

        /// <summary>
        /// Spawn a tool on a tool stack for a specific device (e.g. right hand).
        /// </summary>
        /// <param name="toolType">The tool to spawn</param>
        /// <param name="usedDevices">A list of the used devices coming from the action map</param>
        /// <param name="device">The input device whose tool stack the tool should be spawned on (optional). If not
        /// specified, then it uses the action map to determine which devices the tool should be spawned on.</param>
        /// <param name="rayOrigin">The ray origin on which to spawn th tool</param>
        /// <param name="deviceInputModule">The device input module, if it exists</param>
        /// <returns> Returns tool that was spawned or null if the spawn failed.</returns>
        public ToolData SpawnTool(Type toolType, out HashSet<InputDevice> usedDevices, InputDevice device, Transform rayOrigin,
            DeviceInputModule deviceInputModule)
        {
            usedDevices = new HashSet<InputDevice>();
            if (!typeof(ITool).IsAssignableFrom(toolType))
            {
                Debug.LogWarningFormat("Cannot spawn {0} which is not an ITool", toolType.Name);
                return null;
            }

            var deviceSlots = new HashSet<DeviceSlot>();
            var tool = EditorXRUtils.AddComponent(toolType, gameObject) as ITool;
            ActionMapInput actionMapInput = null;
            if (deviceInputModule != null)
            {
                actionMapInput = deviceInputModule.CreateActionMapInputForObject(tool, device);
                if (actionMapInput != null)
                {
                    usedDevices.UnionWith(actionMapInput.GetCurrentlyUsedDevices());
                    InputUtils.CollectDeviceSlotsFromActionMapInput(actionMapInput, ref deviceSlots);

                    actionMapInput.Reset(false);
                }
            }

            if (usedDevices.Count == 0)
                usedDevices.Add(device);

            this.InjectFunctionalitySingle(tool);
            this.ConnectInterfaces(tool, rayOrigin);

            var icon = tool as IMenuIcon;
            return new ToolData { tool = tool, input = actionMapInput, icon = icon != null ? icon.icon : null };
        }

        void AddToolToDeviceData(ToolData toolData, HashSet<InputDevice> devices)
        {
            foreach (var device in deviceData)
            {
                if (devices.Contains(device.inputDevice))
                    AddToolToStack(device, toolData);
            }
        }

        public bool IsToolActive(Transform targetRayOrigin, Type toolType)
        {
            var result = false;

            var device = deviceData.FirstOrDefault(dd => dd.rayOrigin == targetRayOrigin);
            if (device != null)
                result = device.currentTool.GetType() == toolType;

            return result;
        }

        public bool SelectTool(Transform rayOrigin, Type toolType, bool despawnOnReselect = true, bool hideMenu = false)
        {
            var deviceInputModule = ModuleLoaderCore.instance.GetModule<DeviceInputModule>();
            var result = false;
            m_RayModule.ForEachProxyDevice(device =>
            {
                if (device.rayOrigin == rayOrigin)
                {
                    var spawnTool = true;
                    var currentTool = device.currentTool;
                    var currentToolType = currentTool.GetType();
                    var currentToolIsSelect = currentToolType == typeof(SelectionTool);
                    var setSelectAsCurrentToolOnDespawn = toolType == typeof(SelectionTool) && !currentToolIsSelect;
                    var toolsMenu = device.toolsMenu;

                    // If this tool was on the current device already, remove it, if it is selected while already being the current tool
                    var despawn = (!currentToolIsSelect && currentToolType == toolType && despawnOnReselect) || setSelectAsCurrentToolOnDespawn;
                    if (currentTool != null && despawn)
                    {
                        DespawnTool(device, currentTool);

                        if (!setSelectAsCurrentToolOnDespawn)
                        {
                            // Delete a button of the first type parameter
                            // Then select a button the second type param (the new current tool)
                            // Don't spawn a new tool, since we are only removing the old tool
                            toolsMenu.deleteToolsMenuButton(toolType, currentToolType);
                        }
                        else if (setSelectAsCurrentToolOnDespawn)
                        {
                            // Set the selection tool as the active tool, if select is to be the new current tool
                            toolsMenu.setButtonForType(typeof(SelectionTool), null);
                        }

                        spawnTool = false;
                    }

                    if (spawnTool && !IsDefaultTool(toolType))
                    {
                        // Spawn tool and collect all devices that this tool will need
                        HashSet<InputDevice> usedDevices;
                        var inputDevice = device.inputDevice;
                        var newTool = SpawnTool(toolType, out usedDevices, inputDevice, rayOrigin, deviceInputModule);
                        var multiTool = newTool.tool as IMultiDeviceTool;
                        if (multiTool != null)
                        {
                            multiTool.primary = true;
                            m_RayModule.ForEachProxyDevice(otherDeviceData =>
                            {
                                if (otherDeviceData != device)
                                {
                                    HashSet<InputDevice> otherUsedDevices;
                                    var otherToolData = SpawnTool(toolType, out otherUsedDevices, otherDeviceData.inputDevice, otherDeviceData.rayOrigin, deviceInputModule);
                                    foreach (var dd in deviceData)
                                    {
                                        if (!otherUsedDevices.Contains(dd.inputDevice))
                                            continue;

                                        var otherCurrentTool = otherDeviceData.currentTool;
                                        if (otherCurrentTool != null) // Remove the current tool on all devices this tool will be spawned on
                                            DespawnTool(otherDeviceData, otherCurrentTool);

                                        AddToolToStack(dd, otherToolData);
                                    }
                                }
                            });
                        }

                        // Exclusive mode tools always take over all tool stacks
                        if (newTool.tool is IExclusiveMode)
                        {
                            foreach (var dev in deviceData)
                            {
                                usedDevices.Add(dev.inputDevice);
                            }
                        }

                        foreach (var data in deviceData)
                        {
                            if (!usedDevices.Contains(data.inputDevice))
                                continue;

                            if (currentTool != null) // Remove the current tool on all devices this tool will be spawned on
                                DespawnTool(device, currentTool);

                            AddToolToStack(data, newTool);

                            toolsMenu.setButtonForType(toolType, newTool.icon);
                        }
                    }

                    deviceInputModule.UpdatePlayerHandleMaps();
                    result = spawnTool;
                }
                else if (hideMenu)
                {
                    device.menuHideData[device.mainMenu].hideFlags |= MenuHideFlags.Hidden;
                }
            });

            return result;
        }

        void DespawnTool(DeviceData device, ITool tool)
        {
            var toolType = tool.GetType();
            if (!IsDefaultTool(toolType))
            {
                // Remove the tool if it is the current tool on this device tool stack
                if (device.currentTool == tool)
                {
                    var topTool = device.toolData.Peek();
                    if (topTool == null || topTool.tool != device.currentTool)
                    {
                        Debug.LogError("Tool at top of stack is not current tool.");
                        return;
                    }

                    if (device.customMenu != null)
                    {
                        device.menuHideData.Remove(device.customMenu);
                        device.customMenu = null;
                    }

                    var oldTool = device.toolData.Pop();
                    oldTool.input.active = false;
                    topTool = device.toolData.Peek();
                    device.currentTool = topTool.tool;

                    // Pop this tool off any other stack that references it (for single instance tools)
                    foreach (var otherDeviceData in deviceData)
                    {
                        if (otherDeviceData != device)
                        {
                            // Pop this tool off any other stack that references it (for single instance, multi-device tools)
                            var otherTool = otherDeviceData.currentTool;
                            if (otherTool == tool)
                            {
                                oldTool = otherDeviceData.toolData.Pop();
                                oldTool.input.active = false;
                                var otherToolData = otherDeviceData.toolData.Peek();
                                if (otherToolData != null)
                                    otherDeviceData.currentTool = otherToolData.tool;

                                if (tool is IExclusiveMode)
                                    SetToolsEnabled(otherDeviceData, true);
                            }

                            // Pop this tool of any other stack that references it (for IMultiDeviceTools)
                            if (tool is IMultiDeviceTool)
                            {
                                otherDeviceData.toolsMenu.deleteToolsMenuButton(toolType, typeof(SelectionTool));

                                if (otherTool.GetType() == toolType)
                                {
                                    oldTool = otherDeviceData.toolData.Pop();
                                    oldTool.input.active = false;
                                    var otherToolData = otherDeviceData.toolData.Peek();
                                    if (otherToolData != null)
                                    {
                                        otherDeviceData.currentTool = otherToolData.tool;
                                        this.DisconnectInterfaces(otherTool, otherDeviceData.rayOrigin);
                                        UnityObjectUtils.Destroy((MonoBehaviour)otherTool);
                                    }
                                }
                            }

                            // If the tool had a custom menu, the custom menu would spawn on the opposite device
                            var customMenu = otherDeviceData.customMenu;
                            if (customMenu != null)
                            {
                                otherDeviceData.menuHideData.Remove(customMenu);
                                otherDeviceData.customMenu = null;
                            }
                        }
                    }
                }

                this.DisconnectInterfaces(tool, device.rayOrigin);

                // Exclusive tools disable other tools underneath, so restore those
                if (tool is IExclusiveMode)
                    SetToolsEnabled(device, true);

                UnityObjectUtils.Destroy((MonoBehaviour)tool);
            }
        }

        static void SetToolsEnabled(DeviceData deviceData, bool value)
        {
            foreach (var td in deviceData.toolData)
            {
                var mb = td.tool as MonoBehaviour;
                if (mb)
                    mb.enabled = value;
            }
        }

        static void AddToolToStack(DeviceData deviceData, ToolData toolData)
        {
            if (toolData != null)
            {
                // Exclusive tools render other tools disabled while they are on the stack
                if (toolData.tool is IExclusiveMode)
                    SetToolsEnabled(deviceData, false);

                deviceData.toolData.Push(toolData);
                deviceData.currentTool = toolData.tool;
            }
        }

        internal void UpdatePlayerHandleMaps(List<ActionMapInput> maps)
        {
            foreach (var device in deviceData)
            {
                foreach (var td in device.toolData)
                {
                    if (td.input != null && !maps.Contains(td.input))
                        maps.Add(td.input);
                }
            }
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var selectToolSubscriber = obj as IFunctionalitySubscriber<IProvidesSelectTool>;
            if (selectToolSubscriber != null)
                selectToolSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
#endif
