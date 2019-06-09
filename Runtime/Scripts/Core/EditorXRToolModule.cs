#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using Unity.Labs.ModuleLoader;

namespace UnityEditor.Experimental.EditorVR.Core
{
    class ToolData
    {
        public ITool tool;
        public ActionMapInput input;
        public Sprite icon;
    }

    class EditorXRToolModule : MonoBehaviour, IModuleDependency<EditorVR>, IModuleDependency<EditorXRVacuumableModule>,
        IModuleDependency<LockModule>, IModuleDependency<EditorXRMenuModule>, IModuleDependency<DeviceInputModule>,
        IModuleDependency<EditorXRRayModule>, IInterfaceConnector, IConnectInterfaces, IInitializableModule,
        IUsesFunctionalityInjection
    {
        static readonly List<Type> k_AllTools = new List<Type>();

        readonly Dictionary<Type, List<ILinkedObject>> m_LinkedObjects = new Dictionary<Type, List<ILinkedObject>>();
        EditorVR m_EditorVR;
        EditorXRVacuumableModule m_VacuumablesModule;
        LockModule m_LockModule;
        EditorXRMenuModule m_MenuModule;
        DeviceInputModule m_DeviceInputModule;
        EditorXRRayModule m_RayModule;

        internal List<Type> allTools { get { return k_AllTools; } }

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
#endif

        static EditorXRToolModule()
        {
            typeof(ITool).GetImplementationsOfInterface(k_AllTools);
        }

        public void ConnectDependency(EditorVR dependency)
        {
            m_EditorVR = dependency;
        }

        public void ConnectDependency(EditorXRVacuumableModule dependency)
        {
            m_VacuumablesModule = dependency;
        }

        public void ConnectDependency(LockModule dependency)
        {
            m_LockModule = dependency;
        }

        public void ConnectDependency(EditorXRMenuModule dependency)
        {
            m_MenuModule = dependency;
        }

        public void ConnectDependency(DeviceInputModule dependency)
        {
            m_DeviceInputModule = dependency;
        }

        public void ConnectDependency(EditorXRRayModule dependency)
        {
            m_RayModule = dependency;
        }

        public void LoadModule()
        {
            ILinkedObjectMethods.isSharedUpdater = IsSharedUpdater;
            ISelectToolMethods.selectTool = SelectTool;
            ISelectToolMethods.isToolActive = IsToolActive;
        }

        public void UnloadModule()
        {
            m_LinkedObjects.Clear();
        }

        public void Initialize()
        {
            m_LinkedObjects.Clear();
        }

        public void Shutdown()
        {
            m_LinkedObjects.Clear();
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
            return EditorVR.DefaultTools.Contains(type);
        }

        internal void SpawnDefaultTools(IProxy proxy)
        {
            var defaultTools = EditorVR.DefaultTools;

            foreach (var deviceData in m_EditorVR.deviceData)
            {
                var inputDevice = deviceData.inputDevice;
                ToolData selectionToolData = null;

                if (deviceData.proxy != proxy)
                    continue;

                var rayOrigin = deviceData.rayOrigin;
                foreach (var toolType in defaultTools)
                {
                    HashSet<InputDevice> devices;
                    var toolData = SpawnTool(toolType, out devices, inputDevice, rayOrigin);
                    AddToolToDeviceData(toolData, devices);

                    var tool = toolData.tool;
                    var selectionTool = tool as SelectionTool;
                    if (selectionTool)
                    {
                        selectionToolData = toolData;
                        selectionTool.hovered += m_LockModule.OnHovered;
                    }

                    var vacuumTool = tool as VacuumTool;
                    if (vacuumTool)
                    {
                        vacuumTool.defaultOffset = WorkspaceModule.DefaultWorkspaceOffset;
                        vacuumTool.defaultTilt = WorkspaceModule.DefaultWorkspaceTilt;
                        vacuumTool.vacuumables = m_VacuumablesModule.vacuumables;
                    }
                }

                IMainMenu mainMenu;
                var menuHideData = deviceData.menuHideData;
                if (EditorVR.DefaultMenu != null)
                {
                    mainMenu = (IMainMenu)m_MenuModule.SpawnMenu(EditorVR.DefaultMenu, rayOrigin);
                    deviceData.mainMenu = mainMenu;
                    menuHideData[mainMenu] = new MenuHideData();
                }

                if (EditorVR.DefaultAlternateMenu != null)
                {
                    var alternateMenu = (IAlternateMenu)m_MenuModule.SpawnMenu(EditorVR.DefaultAlternateMenu, rayOrigin);
                    menuHideData[alternateMenu] = new MenuHideData();
                    var radialMenu = alternateMenu as RadialMenu;
                    if (radialMenu)
                        radialMenu.itemWasSelected += m_MenuModule.UpdateAlternateMenuOnSelectionChanged;
                }

                var undoMenu = m_MenuModule.SpawnMenu<UndoMenu>(rayOrigin);
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
                deviceData.toolsMenu = toolsMenu;
                toolsMenu.rayOrigin = rayOrigin;
                toolsMenu.setButtonForType(typeof(IMainMenu), null);
                toolsMenu.setButtonForType(typeof(SelectionTool), selectionToolData != null ? selectionToolData.icon : null);

                var spatialMenu = EditorXRUtils.AddComponent<SpatialMenu>(gameObject);
                this.ConnectInterfaces(spatialMenu, rayOrigin);
                spatialMenu.Setup();
                deviceData.spatialMenu = spatialMenu;
            }

            m_DeviceInputModule.UpdatePlayerHandleMaps();
        }

        /// <summary>
        /// Spawn a tool on a tool stack for a specific device (e.g. right hand).
        /// </summary>
        /// <param name="toolType">The tool to spawn</param>
        /// <param name="usedDevices">A list of the used devices coming from the action map</param>
        /// <param name="device">The input device whose tool stack the tool should be spawned on (optional). If not
        /// specified, then it uses the action map to determine which devices the tool should be spawned on.</param>
        /// <returns> Returns tool that was spawned or null if the spawn failed.</returns>
        ToolData SpawnTool(Type toolType, out HashSet<InputDevice> usedDevices, InputDevice device, Transform rayOrigin)
        {
            usedDevices = new HashSet<InputDevice>();
            if (!typeof(ITool).IsAssignableFrom(toolType))
                return null;

            var deviceSlots = new HashSet<DeviceSlot>();
            var tool = EditorXRUtils.AddComponent(toolType, gameObject) as ITool;
            var actionMapInput = m_DeviceInputModule.CreateActionMapInputForObject(tool, device);
            if (actionMapInput != null)
            {
                usedDevices.UnionWith(actionMapInput.GetCurrentlyUsedDevices());
                InputUtils.CollectDeviceSlotsFromActionMapInput(actionMapInput, ref deviceSlots);

                actionMapInput.Reset(false);
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
            foreach (var dd in m_EditorVR.deviceData)
            {
                if (devices.Contains(dd.inputDevice))
                    AddToolToStack(dd, toolData);
            }
        }

        bool IsToolActive(Transform targetRayOrigin, Type toolType)
        {
            var result = false;

            var deviceData = m_EditorVR.deviceData.FirstOrDefault(dd => dd.rayOrigin == targetRayOrigin);
            if (deviceData != null)
                result = deviceData.currentTool.GetType() == toolType;

            return result;
        }

        internal bool SelectTool(Transform rayOrigin, Type toolType, bool despawnOnReselect = true, bool hideMenu = false)
        {
            var result = false;
            m_RayModule.ForEachProxyDevice(deviceData =>
            {
                if (deviceData.rayOrigin == rayOrigin)
                {
                    var spawnTool = true;
                    var currentTool = deviceData.currentTool;
                    var currentToolType = currentTool.GetType();
                    var currentToolIsSelect = currentToolType == typeof(SelectionTool);
                    var setSelectAsCurrentToolOnDespawn = toolType == typeof(SelectionTool) && !currentToolIsSelect;
                    var toolsMenu = deviceData.toolsMenu;

                    // If this tool was on the current device already, remove it, if it is selected while already being the current tool
                    var despawn = (!currentToolIsSelect && currentToolType == toolType && despawnOnReselect) || setSelectAsCurrentToolOnDespawn;
                    if (currentTool != null && despawn)
                    {
                        DespawnTool(deviceData, currentTool);

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
                        var evrDeviceData = m_EditorVR.deviceData;

                        // Spawn tool and collect all devices that this tool will need
                        HashSet<InputDevice> usedDevices;
                        var device = deviceData.inputDevice;
                        var newTool = SpawnTool(toolType, out usedDevices, device, rayOrigin);
                        var multiTool = newTool.tool as IMultiDeviceTool;
                        if (multiTool != null)
                        {
                            multiTool.primary = true;
                            m_RayModule.ForEachProxyDevice(otherDeviceData =>
                            {
                                if (otherDeviceData != deviceData)
                                {
                                    HashSet<InputDevice> otherUsedDevices;
                                    var otherToolData = SpawnTool(toolType, out otherUsedDevices, otherDeviceData.inputDevice, otherDeviceData.rayOrigin);
                                    foreach (var dd in evrDeviceData)
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
                            foreach (var dev in evrDeviceData)
                            {
                                usedDevices.Add(dev.inputDevice);
                            }
                        }

                        foreach (var data in evrDeviceData)
                        {
                            if (!usedDevices.Contains(data.inputDevice))
                                continue;

                            if (currentTool != null) // Remove the current tool on all devices this tool will be spawned on
                                DespawnTool(deviceData, currentTool);

                            AddToolToStack(data, newTool);

                            toolsMenu.setButtonForType(toolType, newTool.icon);
                        }
                    }

                    m_DeviceInputModule.UpdatePlayerHandleMaps();
                    result = spawnTool;
                }
                else if (hideMenu)
                {
                    deviceData.menuHideData[deviceData.mainMenu].hideFlags |= MenuHideFlags.Hidden;
                }
            });

            return result;
        }

        void DespawnTool(DeviceData deviceData, ITool tool)
        {
            var toolType = tool.GetType();
            if (!IsDefaultTool(toolType))
            {
                // Remove the tool if it is the current tool on this device tool stack
                if (deviceData.currentTool == tool)
                {
                    var topTool = deviceData.toolData.Peek();
                    if (topTool == null || topTool.tool != deviceData.currentTool)
                    {
                        Debug.LogError("Tool at top of stack is not current tool.");
                        return;
                    }

                    if (deviceData.customMenu != null)
                    {
                        deviceData.menuHideData.Remove(deviceData.customMenu);
                        deviceData.customMenu = null;
                    }

                    var oldTool = deviceData.toolData.Pop();
                    oldTool.input.active = false;
                    topTool = deviceData.toolData.Peek();
                    deviceData.currentTool = topTool.tool;

                    // Pop this tool off any other stack that references it (for single instance tools)
                    foreach (var otherDeviceData in m_EditorVR.deviceData)
                    {
                        if (otherDeviceData != deviceData)
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

                this.DisconnectInterfaces(tool, deviceData.rayOrigin);

                // Exclusive tools disable other tools underneath, so restore those
                if (tool is IExclusiveMode)
                    SetToolsEnabled(deviceData, true);

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
            foreach (var deviceData in m_EditorVR.deviceData)
            {
                foreach (var td in deviceData.toolData)
                {
                    if (td.input != null && !maps.Contains(td.input))
                        maps.Add(td.input);
                }
            }
        }
    }
}
#endif
