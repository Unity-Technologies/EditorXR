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

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		class Tools : Nested
		{

			internal class ToolData
			{
				public ITool tool;
				public ActionMapInput input;
			}

			internal List<Type> allTools { get; private set; }

			internal Dictionary<Type, List<ILinkedObject>> linkedObjects { get { return m_LinkedObjects; } }
			readonly Dictionary<Type, List<ILinkedObject>> m_LinkedObjects = new Dictionary<Type, List<ILinkedObject>>();

			internal Tools()
			{
				allTools = ObjectUtils.GetImplementationsOfInterface(typeof(ITool)).ToList();
			}

			internal bool IsPermanentTool(Type type)
			{
				return typeof(ITransformer).IsAssignableFrom(type)
					|| typeof(SelectionTool).IsAssignableFrom(type)
					|| typeof(ILocomotor).IsAssignableFrom(type)
					|| typeof(VacuumTool).IsAssignableFrom(type);
			}

			internal void SpawnDefaultTools(IProxy proxy)
			{
				// Spawn default tools
				HashSet<InputDevice> devices;

				var transformTool = SpawnTool(typeof(TransformTool), out devices);
				evr.m_DirectSelection.objectsGrabber = transformTool.tool as IGrabObjects;

				foreach (var deviceData in evr.m_DeviceData)
				{
					var inputDevice = deviceData.inputDevice;

					if (deviceData.proxy != proxy)
						continue;

					var toolData = SpawnTool(typeof(SelectionTool), out devices, inputDevice);
					AddToolToDeviceData(toolData, devices);
					var selectionTool = (SelectionTool)toolData.tool;
					selectionTool.hovered += evr.m_LockModule.OnHovered;
					selectionTool.isRayActive = evr.m_Rays.IsRayActive;

					toolData = SpawnTool(typeof(VacuumTool), out devices, inputDevice);
					AddToolToDeviceData(toolData, devices);
					var vacuumTool = (VacuumTool)toolData.tool;
					vacuumTool.defaultOffset = WorkspaceModule.k_DefaultWorkspaceOffset;
					vacuumTool.defaultTilt = WorkspaceModule.k_DefaultWorkspaceTilt;
					vacuumTool.vacuumables = evr.m_Vacuumables.vacuumables;

					// Using a shared instance of the transform tool across all device tool stacks
					AddToolToStack(deviceData, transformTool);

					toolData = SpawnTool(typeof(BlinkLocomotionTool), out devices, inputDevice);
					AddToolToDeviceData(toolData, devices);

					var evrMenus = evr.m_Menus;
					var mainMenu = evrMenus.SpawnMainMenu(typeof(MainMenu), inputDevice, false, out deviceData.mainMenuInput);
					deviceData.mainMenu = mainMenu;
					deviceData.menuHideFlags[mainMenu] = Menus.MenuHideFlags.Hidden;

					var mainMenuActivator = evrMenus.SpawnMainMenuActivator(inputDevice);
					deviceData.mainMenuActivator = mainMenuActivator;
					mainMenuActivator.selected += evrMenus.OnMainMenuActivatorSelected;
					mainMenuActivator.hoverStarted += evrMenus.OnMainMenuActivatorHoverStarted;
					mainMenuActivator.hoverEnded += evrMenus.OnMainMenuActivatorHoverEnded;

					var toolButtonActivePosition = new Vector3(0f, 0f, -0.035f); // Frontmost active button offset from the main menu activator
					var selectionToolButton = evrMenus.SpawnPinnedToolButton(inputDevice);
					var selectionToolButtonTransform = selectionToolButton.transform;
					deviceData.pinnedToolButtons = new List<PinnedToolButton>();
					deviceData.selectionToolButton = selectionToolButton;
					selectionToolButtonTransform.SetParent(mainMenuActivator.transform, false);
					selectionToolButton.activePosition = toolButtonActivePosition;
					selectionToolButton.toolType = typeof(SelectionTool); // Selection tool is visible & persistent by default
					selectionToolButton.node = deviceData.node;

					var alternateMenu = evrMenus.SpawnAlternateMenu(typeof(RadialMenu), inputDevice, out deviceData.alternateMenuInput);
					deviceData.alternateMenu = alternateMenu;
					deviceData.menuHideFlags[alternateMenu] = Menus.MenuHideFlags.Hidden;
					alternateMenu.itemWasSelected += evrMenus.UpdateAlternateMenuOnSelectionChanged;
				}

				evr.m_DeviceInputModule.UpdatePlayerHandleMaps();
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
				var tool = ObjectUtils.AddComponent(toolType, evr.gameObject) as ITool;

				var actionMapInput = evr.m_DeviceInputModule.CreateActionMapInputForObject(tool, device);
				if (actionMapInput != null)
				{
					usedDevices.UnionWith(actionMapInput.GetCurrentlyUsedDevices());
					InputUtils.CollectDeviceSlotsFromActionMapInput(actionMapInput, ref deviceSlots);
				}

				evr.m_Interfaces.ConnectInterfaces(tool, device);

				return new ToolData { tool = tool, input = actionMapInput };
			}

			void AddToolToDeviceData(ToolData toolData, HashSet<InputDevice> devices)
			{
				foreach (var dd in evr.m_DeviceData)
				{
					if (devices.Contains(dd.inputDevice))
						AddToolToStack(dd, toolData);
				}
			}

			internal bool IsToolActive(Transform targetRayOrigin, Type toolType)
			{
				var result = false;

				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == targetRayOrigin);
				if (deviceData != null)
					result = deviceData.currentTool.GetType() == toolType;

				return result;
			}

			internal bool SelectTool(Transform rayOrigin, Type toolType)
			{
				//Debug.LogError("SelectionTool TYPE : <color=black>" + toolType.ToString() + "</color>");
				//if (toolType == typeof(SelectionTool))
					//Debug.LogError("<color=green>!!!!! SelectionTool detected</color>");

				var result = false;
				evr.m_Rays.ForEachProxyDevice((deviceData) =>
				{
					if (deviceData.rayOrigin == rayOrigin)
					{
						Debug.LogError("<color=yellow>deviceDate.CurrentTool : </color>" + deviceData.currentTool.ToString());
						var spawnTool = true;
						var setSelectAsCurrentTool = toolType == typeof(SelectionTool);//deviceData.currentTool is ILocomotor;

						// If this tool was on the current device already, then simply remove it
						if (deviceData.currentTool != null && (deviceData.currentTool.GetType() == toolType || setSelectAsCurrentTool))
						{
							Debug.LogError("Despawing tool !!!! : <color=red>toolType == typeof(SelectionTool) : </color>" + (toolType == typeof(SelectionTool)).ToString());
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

							// It's possible this tool uses no action maps, so at least include the device this tool was spawned on
							if (usedDevices.Count == 0)
								usedDevices.Add(device);

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

								if (setSelectAsCurrentTool)
									deviceData.selectionToolButton.toolType = toolType; // assign the selection tool to the selection tool button
								else
									AddPinnedToolButton(deviceData, toolType);

								//deviceData.pinnedToolButtons.rayOrigin = rayOrigin;
								deviceData.selectionToolButton.rayOrigin = rayOrigin;
							}
						}

						SetupPinnedToolButtonsForDevice(deviceData, rayOrigin, toolType);
						evr.m_DeviceInputModule.UpdatePlayerHandleMaps();
						result = spawnTool;
					}
					else
					{
						deviceData.menuHideFlags[deviceData.mainMenu] |= Menus.MenuHideFlags.Hidden;
					}
				});

				return result;
			}

			void AddPinnedToolButton(DeviceData deviceData, Type toolType)
			{
				// CHECK FOR TOOL ALREADY ADDED AS AN EXISTING PIN, RETURN THE BUTTON GRADIENT
				foreach (var button in deviceData.pinnedToolButtons)
				{
					if (button.toolType == toolType)
						return;
				}

				var evrMenus = evr.m_Menus;
				var pinnedToolButton = evrMenus.SpawnPinnedToolButton(deviceData.inputDevice);
				var pinnedToolButtonTransform = pinnedToolButton.transform;
				deviceData.pinnedToolButtons.Add(pinnedToolButton);
				pinnedToolButtonTransform.SetParent(deviceData.mainMenuActivator.transform, false);
				pinnedToolButton.activePosition = deviceData.selectionToolButton.activePosition;
				pinnedToolButton.node = deviceData.node;
				pinnedToolButton.activeTool = true;
				pinnedToolButton.toolType = toolType; // assign/setup the new current tool type in the new tool button
				deviceData.selectionToolButton.activeTool = false;
			}

			PinnedToolButton SetupPinnedToolButtonsForDevice(DeviceData deviceData, Transform rayOrigin, Type activeToolType)
			{
				PinnedToolButton activeButton = null;

				if (deviceData.currentTool is ILocomotor) // Verify that Selection is not already the currently active tool
				{
					Debug.LogError("<color=orange>currentToolIsSelect <--</color>");
					activeButton = deviceData.selectionToolButton;
					activeButton.activeTool = true;
					foreach (var button in deviceData.pinnedToolButtons)
					{
						button.rayOrigin = rayOrigin;
						button.activeTool = false;
					}
				}
				else
				{
					// activeButton = ITERATE OVER ALL TOOL BUTTONS except for selection
					deviceData.selectionToolButton.activeTool = false;
					foreach (var button in deviceData.pinnedToolButtons)
					{
						button.rayOrigin = rayOrigin;
						button.activeTool = button.toolType == activeToolType;

						if (button.activeTool)
							activeButton = button;
					}
					//deviceData.pinnedToolButtons.activeTool = true; // ITERATE OVER ALL TOOL BUTTONS except for selection
					// Iternate over the pinnedToolButtons array/list searching for the active tool
				}

				Debug.LogError("<color=orange>deviceDate.CurrentTool : </color>" + deviceData.currentTool.ToString() + "  - selection is current tool : " + (activeButton == deviceData.selectionToolButton));
				deviceData.proxy.HighlightDevice(deviceData.node, activeButton.gradientPair); // Perform the higlight on the node with the button's gradient pair

				return activeButton;
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

						// Pop this tool off any other stack that references it (for single instance tools)
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

			void SetToolsEnabled(DeviceData deviceData, bool value)
			{
				foreach (var td in deviceData.toolData)
				{
					var mb = td.tool as MonoBehaviour;
					mb.enabled = value;
				}
			}

			void AddToolToStack(DeviceData deviceData, ToolData toolData)
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
				maps.AddRange(evr.m_MiniWorlds.inputs.Values);

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

				maps.Add(evr.m_DeviceInputModule.trackedObjectInput);

				foreach (var deviceData in evrDeviceData)
				{
					foreach (var td in deviceData.toolData)
					{
						if (td.input != null && !maps.Contains(td.input))
							maps.Add(td.input);
					}
				}
			}

			internal PinnedToolButton PreviewToolInPinnedToolButton (Transform rayOrigin, Type toolType)
			{
				// Prevents menu buttons of types other than ITool from triggering any pinned tool button preview actions
				if (!toolType.GetInterfaces().Contains(typeof(ITool)))
					return null;

				PinnedToolButton pinnedToolButton = null;
				evr.m_Rays.ForEachProxyDevice((deviceData) =>
				{
					if (deviceData.rayOrigin == rayOrigin) // enable pinned tool preview on the opposite (handed) device
					{
						pinnedToolButton = deviceData.selectionToolButton.activeTool ? deviceData.selectionToolButton : deviceData.pinnedToolButtons.FirstOrDefault( (x) => x.activeTool);
						pinnedToolButton.previewToolType = toolType;
					}
				});

				return pinnedToolButton;
			}
		}
	}
}
#endif
