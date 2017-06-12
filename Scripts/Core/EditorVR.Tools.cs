#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class Tools : Nested, IInterfaceConnector
		{
			internal class ToolData
			{
				public ITool tool;
				public ActionMapInput input;
			}

			internal List<Type> allTools { get; private set; }

			readonly Dictionary<Type, List<ILinkedObject>> m_LinkedObjects = new Dictionary<Type, List<ILinkedObject>>();

			public Tools()
			{
				allTools = ObjectUtils.GetImplementationsOfInterface(typeof(ITool)).ToList();

				ILinkedObjectMethods.isSharedUpdater = IsSharedUpdater;
				ISelectToolMethods.selectTool = SelectTool;
				ISelectToolMethods.isToolActive = IsToolActive;
			}

			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var linkedObject = obj as ILinkedObject;
				if (linkedObject != null)
				{
					var type = obj.GetType();
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

			public void DisconnectInterface(object obj)
			{
			}

			bool IsSharedUpdater(ILinkedObject linkedObject)
			{
				var type = linkedObject.GetType();
				return m_LinkedObjects[type].IndexOf(linkedObject) == 0;
			}

			internal static bool IsDefaultTool(Type type)
			{
				return evr.m_DefaultTools.Contains(type);
			}

			internal static void SpawnDefaultTools(IProxy proxy)
			{
				Func<Transform, bool> isRayActive = Rays.IsRayActive;
				var vacuumables = evr.GetNestedModule<Vacuumables>();
				var lockModule = evr.GetModule<LockModule>();
				var defaultTools = evr.m_DefaultTools;

				foreach (var deviceData in evr.m_DeviceData)
				{
					var inputDevice = deviceData.inputDevice;

					if (deviceData.proxy != proxy)
						continue;

					foreach (var toolType in defaultTools)
					{
						HashSet<InputDevice> devices;
						var toolData = SpawnTool(toolType, out devices, inputDevice);
						AddToolToDeviceData(toolData, devices);

						var tool = toolData.tool;
						var selectionTool = tool as SelectionTool;
						if (selectionTool)
						{
							selectionTool.hovered += lockModule.OnHovered;
							selectionTool.isRayActive = isRayActive;
						}

						var vacuumTool = tool as VacuumTool;
						if (vacuumTool)
						{
							vacuumTool.defaultOffset = WorkspaceModule.DefaultWorkspaceOffset;
							vacuumTool.defaultTilt = WorkspaceModule.DefaultWorkspaceTilt;
							vacuumTool.vacuumables = vacuumables.vacuumables;
						}
					}

					var mainMenu = Menus.SpawnMainMenu(typeof(MainMenu), inputDevice, false, out deviceData.mainMenuInput);
					deviceData.mainMenu = mainMenu;
					deviceData.menuHideFlags[mainMenu] = Menus.MenuHideFlags.Hidden;

					var mainMenuActivator = Menus.SpawnMainMenuActivator(inputDevice);
					deviceData.mainMenuActivator = mainMenuActivator;
					mainMenuActivator.selected += Menus.OnMainMenuActivatorSelected;
					mainMenuActivator.hoverStarted += Menus.OnMainMenuActivatorHoverStarted;
					mainMenuActivator.hoverEnded += Menus.OnMainMenuActivatorHoverEnded;

					var pinnedToolButton = Menus.SpawnPinnedToolButton(inputDevice);
					deviceData.previousToolButton = pinnedToolButton;
					var pinnedToolButtonTransform = pinnedToolButton.transform;
					pinnedToolButtonTransform.SetParent(mainMenuActivator.transform, false);
					pinnedToolButtonTransform.localPosition = new Vector3(0f, 0f, -0.035f); // Offset from the main menu activator

					var alternateMenu = Menus.SpawnAlternateMenu(typeof(RadialMenu), inputDevice, out deviceData.alternateMenuInput);
					deviceData.alternateMenu = alternateMenu;
					deviceData.menuHideFlags[alternateMenu] = Menus.MenuHideFlags.Hidden;
					alternateMenu.itemWasSelected += Menus.UpdateAlternateMenuOnSelectionChanged;
				}

				evr.GetModule<DeviceInputModule>().UpdatePlayerHandleMaps();
			}

			/// <summary>
			/// Spawn a tool on a tool stack for a specific device (e.g. right hand).
			/// </summary>
			/// <param name="toolType">The tool to spawn</param>
			/// <param name="usedDevices">A list of the used devices coming from the action map</param>
			/// <param name="device">The input device whose tool stack the tool should be spawned on (optional). If not
			/// specified, then it uses the action map to determine which devices the tool should be spawned on.</param>
			/// <returns> Returns tool that was spawned or null if the spawn failed.</returns>
			static ToolData SpawnTool(Type toolType, out HashSet<InputDevice> usedDevices, InputDevice device = null)
			{
				usedDevices = new HashSet<InputDevice>();
				if (!typeof(ITool).IsAssignableFrom(toolType))
					return null;

				var deviceSlots = new HashSet<DeviceSlot>();
				var tool = ObjectUtils.AddComponent(toolType, evr.gameObject) as ITool;

				var actionMapInput = evr.GetModule<DeviceInputModule>().CreateActionMapInputForObject(tool, device);
				if (actionMapInput != null)
				{
					usedDevices.UnionWith(actionMapInput.GetCurrentlyUsedDevices());
					InputUtils.CollectDeviceSlotsFromActionMapInput(actionMapInput, ref deviceSlots);
				}

				if (usedDevices.Count == 0)
					usedDevices.Add(device);

				evr.m_Interfaces.ConnectInterfaces(tool, device);

				return new ToolData { tool = tool, input = actionMapInput };
			}

			static void AddToolToDeviceData(ToolData toolData, HashSet<InputDevice> devices)
			{
				foreach (var dd in evr.m_DeviceData)
				{
					if (devices.Contains(dd.inputDevice))
						AddToolToStack(dd, toolData);
				}
			}

			static bool IsToolActive(Transform targetRayOrigin, Type toolType)
			{
				var result = false;

				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == targetRayOrigin);
				if (deviceData != null)
					result = deviceData.currentTool.GetType() == toolType;

				return result;
			}

			static bool SelectTool(Transform rayOrigin, Type toolType)
			{
				var result = false;
				var deviceInputModule = evr.GetModule<DeviceInputModule>();
				Rays.ForEachProxyDevice(deviceData =>
				{
					if (deviceData.rayOrigin == rayOrigin)
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
							var device = deviceData.inputDevice;
							var newTool = SpawnTool(toolType, out usedDevices, device);

							var evrDeviceData = evr.m_DeviceData;

							// Exclusive mode tools always take over all tool stacks
							if (newTool is IExclusiveMode)
							{
								foreach (var dev in evrDeviceData)
								{
									usedDevices.Add(dev.inputDevice);
								}
							}

							foreach (var dd in evrDeviceData)
							{
								if (!usedDevices.Contains(dd.inputDevice))
									continue;

								if (deviceData.currentTool != null) // Remove the current tool on all devices this tool will be spawned on
									DespawnTool(deviceData, deviceData.currentTool);

								AddToolToStack(dd, newTool);
							}
						}

						deviceData.previousToolButton.rayOrigin = rayOrigin;
						deviceData.previousToolButton.toolType = toolType; // assign the new current tool type to the active tool button

						deviceInputModule.UpdatePlayerHandleMaps();
						result = spawnTool;
					}
					else
					{
						deviceData.menuHideFlags[deviceData.mainMenu] |= Menus.MenuHideFlags.Hidden;
					}
				});

				return result;
			}

			static void DespawnTool(DeviceData deviceData, ITool tool)
			{
				if (!IsDefaultTool(tool.GetType()))
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
						foreach (var otherDeviceData in evr.m_DeviceData)
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
					evr.m_Interfaces.DisconnectInterfaces(tool);

					// Exclusive tools disable other tools underneath, so restore those
					if (tool is IExclusiveMode)
						SetToolsEnabled(deviceData, true);

					ObjectUtils.Destroy(tool as MonoBehaviour);
				}
			}

			static void SetToolsEnabled(DeviceData deviceData, bool value)
			{
				foreach (var td in deviceData.toolData)
				{
					var mb = td.tool as MonoBehaviour;
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

			internal static void UpdatePlayerHandleMaps(List<ActionMapInput> maps)
			{
				foreach (var input in evr.GetModule<WorkspaceModule>().workspaceInputs)
				{
					maps.Add(input);
				}

				var evrDeviceData = evr.m_DeviceData;
				foreach (var deviceData in evrDeviceData)
				{
					var mainMenu = deviceData.mainMenu;
					var mainMenuInput = deviceData.mainMenuInput;
					if (mainMenu != null && mainMenuInput != null)
					{
						mainMenuInput.active = mainMenu.visible;

						if (!maps.Contains(mainMenuInput))
							maps.Add(mainMenuInput);
					}

					var alternateMenu = deviceData.alternateMenu;
					var alternateMenuInput = deviceData.alternateMenuInput;
					if (alternateMenu != null && alternateMenuInput != null)
					{
						alternateMenuInput.active = alternateMenu.visible;

						if (!maps.Contains(alternateMenuInput))
							maps.Add(alternateMenuInput);
					}

					maps.Add(deviceData.directSelectInput);
					maps.Add(deviceData.uiInput);
				}

				maps.Add(evr.GetModule<DeviceInputModule>().trackedObjectInput);

				foreach (var deviceData in evrDeviceData)
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
}
#endif
