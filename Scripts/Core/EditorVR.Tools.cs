//#if !UNITY_EDITORVR
//#pragma warning disable 67, 414, 649
//#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Workspaces;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	internal partial class EditorVR : MonoBehaviour
	{
		class ToolData
		{
			public ITool tool;
			public ActionMapInput input;
		}

		class DeviceData
		{
			public readonly Stack<ToolData> toolData = new Stack<ToolData>();
			public ActionMapInput uiInput;
			public MainMenuActivator mainMenuActivator;
			public ActionMapInput directSelectInput;
			public IMainMenu mainMenu;
			public ActionMapInput mainMenuInput;
			public IAlternateMenu alternateMenu;
			public ActionMapInput alternateMenuInput;
			public ITool currentTool;
			public IMenu customMenu;
			public PinnedToolButton previousToolButton;
			public readonly Dictionary<IMenu, MenuHideFlags> menuHideFlags = new Dictionary<IMenu, MenuHideFlags>();
			public readonly Dictionary<IMenu, float> menuSizes = new Dictionary<IMenu, float>();
		}

		List<Type> m_AllTools;

		readonly Dictionary<InputDevice, DeviceData> m_DeviceData = new Dictionary<InputDevice, DeviceData>();

#if UNITY_EDITORVR
		bool IsPermanentTool(Type type)
		{
			return typeof(ITransformer).IsAssignableFrom(type)
				|| typeof(SelectionTool).IsAssignableFrom(type)
				|| typeof(ILocomotor).IsAssignableFrom(type)
				|| typeof(VacuumTool).IsAssignableFrom(type);
		}

		void SpawnDefaultTools()
		{
			// Spawn default tools
			HashSet<InputDevice> devices;

			var transformTool = SpawnTool(typeof(TransformTool), out devices);
			m_ObjectsGrabber = transformTool.tool as IGrabObjects;

			foreach (var deviceDataPair in m_DeviceData)
			{
				var inputDevice = deviceDataPair.Key;
				var deviceData = deviceDataPair.Value;

				// Skip keyboard, mouse, gamepads. Selection, blink, and vacuum tools should only be on left and right hands (tagged 0 and 1)
				if (inputDevice.tagIndex == -1)
					continue;

				var toolData = SpawnTool(typeof(SelectionTool), out devices, inputDevice);
				AddToolToDeviceData(toolData, devices);
				var selectionTool = (SelectionTool)toolData.tool;
				selectionTool.hovered += m_LockModule.OnHovered;
				selectionTool.isRayActive = IsRayActive;

				toolData = SpawnTool(typeof(VacuumTool), out devices, inputDevice);
				AddToolToDeviceData(toolData, devices);
				var vacuumTool = (VacuumTool)toolData.tool;
				vacuumTool.defaultOffset = kDefaultWorkspaceOffset;
				vacuumTool.vacuumables = m_Vacuumables;

				// Using a shared instance of the transform tool across all device tool stacks
				AddToolToStack(inputDevice, transformTool);

				toolData = SpawnTool(typeof(BlinkLocomotionTool), out devices, inputDevice);
				AddToolToDeviceData(toolData, devices);

				var mainMenuActivator = SpawnMainMenuActivator(inputDevice);
				deviceData.mainMenuActivator = mainMenuActivator;
				mainMenuActivator.selected += OnMainMenuActivatorSelected;
				mainMenuActivator.hoverStarted += OnMainMenuActivatorHoverStarted;
				mainMenuActivator.hoverEnded += OnMainMenuActivatorHoverEnded;

				var pinnedToolButton = SpawnPinnedToolButton(inputDevice);
				deviceData.previousToolButton = pinnedToolButton;
				var pinnedToolButtonTransform = pinnedToolButton.transform;
				pinnedToolButtonTransform.SetParent(mainMenuActivator.transform, false);
				pinnedToolButtonTransform.localPosition = new Vector3(0f, 0f, -0.035f); // Offset from the main menu activator

				var alternateMenu = SpawnAlternateMenu(typeof(RadialMenu), inputDevice, out deviceData.alternateMenuInput);
				deviceData.alternateMenu = alternateMenu;
				deviceData.menuHideFlags[alternateMenu] = MenuHideFlags.Hidden;
				alternateMenu.itemWasSelected += UpdateAlternateMenuOnSelectionChanged;

				UpdatePlayerHandleMaps();
			}
		}

		/// <summary>
		/// Spawn a tool on a tool stack for a specific device (e.g. right hand).
		/// </summary>
		/// <param name="toolType">The tool to spawn</param>
		/// <param name="usedDevices">A list of the used devices coming from the action map</param>
		/// <param name="device">The input device whose tool stack the tool should be spawned on (optional). If not
		/// specified, then it uses the action map to determine which devices the tool should be spawned on.</param>
		/// <returns> Returns tool that was spawned or null if the spawn failed.</returns>
		ToolData SpawnTool(Type toolType, out HashSet<InputDevice> usedDevices, InputDevice device = null)
		{
			usedDevices = new HashSet<InputDevice>();
			if (!typeof(ITool).IsAssignableFrom(toolType))
				return null;

			var deviceSlots = new HashSet<DeviceSlot>();
			var tool = U.Object.AddComponent(toolType, gameObject) as ITool;

			var actionMapInput = CreateActionMapInputForObject(tool, device);
			if (actionMapInput != null)
			{
				usedDevices.UnionWith(actionMapInput.GetCurrentlyUsedDevices());
				U.Input.CollectDeviceSlotsFromActionMapInput(actionMapInput, ref deviceSlots);
			}

			ConnectInterfaces(tool, device);

			return new ToolData { tool = tool, input = actionMapInput };
		}

		void AddToolToDeviceData(ToolData toolData, HashSet<InputDevice> devices)
		{
			foreach (var dev in devices)
				AddToolToStack(dev, toolData);
		}

		bool IsToolActive(Transform targetRayOrigin, Type toolType)
		{
			var result = false;

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == targetRayOrigin)
					result = deviceData.currentTool.GetType() == toolType;
			});

			return result;
		}

		bool SelectTool(Transform rayOrigin, Type toolType)
		{
			var result = false;
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
				{
					var spawnTool = true;

					// If this tool was on the current device already, then simply remove it
					if (deviceData.currentTool != null && deviceData.currentTool.GetType() == toolType)
					{
						DespawnTool(deviceData, deviceData.currentTool);

						// Don't spawn a new tool, since we are only removing the old tool
						spawnTool = false;
					}

					if (spawnTool)
					{
						// Spawn tool and collect all devices that this tool will need
						HashSet<InputDevice> usedDevices;
						var newTool = SpawnTool(toolType, out usedDevices, device);

						// It's possible this tool uses no action maps, so at least include the device this tool was spawned on
						if (usedDevices.Count == 0)
							usedDevices.Add(device);

						// Exclusive mode tools always take over all tool stacks
						if (newTool is IExclusiveMode)
						{
							foreach (var dev in m_DeviceData.Keys)
							{
								usedDevices.Add(dev);
							}
						}

						foreach (var dev in usedDevices)
						{
							deviceData = m_DeviceData[dev];
							if (deviceData.currentTool != null) // Remove the current tool on all devices this tool will be spawned on
								DespawnTool(deviceData, deviceData.currentTool);

							AddToolToStack(dev, newTool);

							deviceData.previousToolButton.toolType = toolType; // assign the new current tool type to the active tool button
							deviceData.previousToolButton.rayOrigin = rayOrigin;
						}
					}

					UpdatePlayerHandleMaps();
					result = spawnTool;
				}
				else
				{
					deviceData.menuHideFlags[deviceData.mainMenu] |= MenuHideFlags.Hidden;
				}
			});

			return result;
		}

		void DespawnTool(DeviceData deviceData, ITool tool)
		{
			if (!IsPermanentTool(tool.GetType()))
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

					deviceData.toolData.Pop();
					topTool = deviceData.toolData.Peek();
					deviceData.currentTool = topTool.tool;

					// Pop this tool of any other stack that references it (for single instance tools)
					foreach (var otherDeviceData in m_DeviceData.Values)
					{
						if (otherDeviceData != deviceData)
						{
							if (otherDeviceData.currentTool == tool)
							{
								otherDeviceData.toolData.Pop();
								var otherToolData = otherDeviceData.toolData.Peek();
								if (otherToolData != null)
									otherDeviceData.currentTool = otherToolData.tool;

								if (tool is IExclusiveMode)
									SetToolsEnabled(otherDeviceData, true);
							}

							// If the tool had a custom menu, the custom menu would spawn on the opposite device
							var customMenu = otherDeviceData.customMenu;
							if (customMenu != null)
							{
								otherDeviceData.menuHideFlags.Remove(customMenu);
								otherDeviceData.customMenu = null;
							}
						}
					}
				}
				DisconnectInterfaces(tool);

				// Exclusive tools disable other tools underneath, so restore those
				if (tool is IExclusiveMode)
					SetToolsEnabled(deviceData, true);

				U.Object.Destroy(tool as MonoBehaviour);
			}
		}

		void SetToolsEnabled(DeviceData deviceData, bool value)
		{
			foreach (var td in deviceData.toolData)
			{
				var mb = td.tool as MonoBehaviour;
				mb.enabled = value;
			}
		}

		void AddToolToStack(InputDevice device, ToolData toolData)
		{
			if (toolData != null)
			{
				var deviceData = m_DeviceData[device];

				// Exclusive tools render other tools disabled while they are on the stack
				if (toolData.tool is IExclusiveMode)
					SetToolsEnabled(deviceData, false);

				deviceData.toolData.Push(toolData);
				deviceData.currentTool = toolData.tool;
			}
		}

#endif
	}
}
