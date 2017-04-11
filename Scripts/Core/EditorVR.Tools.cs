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

			internal static bool IsPermanentTool(Type type)
			{
				return typeof(ITransformer).IsAssignableFrom(type)
					|| typeof(SelectionTool).IsAssignableFrom(type)
					|| typeof(ILocomotor).IsAssignableFrom(type)
					|| typeof(VacuumTool).IsAssignableFrom(type)
					|| typeof(MoveWorkspacesTool).IsAssignableFrom(type);
			}

			internal void SpawnDefaultTools(IProxy proxy)
			{
				// Spawn default tools

				Func<Transform, bool> isRayActive = Rays.IsRayActive;
				var vacuumables = evr.GetNestedModule<Vacuumables>();
				var lockModule = evr.GetModule<LockModule>();

				foreach (var deviceData in evr.m_DeviceData)
				{
					var inputDevice = deviceData.inputDevice;

					if (deviceData.proxy != proxy)
						continue;

					HashSet<InputDevice> devices;
					var toolData = SpawnTool(typeof(SelectionTool), out devices, inputDevice);
					AddToolToDeviceData(toolData, devices);
					var selectionTool = (SelectionTool)toolData.tool;
					selectionTool.hovered += lockModule.OnHovered;
					selectionTool.isRayActive = isRayActive;

					toolData = SpawnTool(typeof(VacuumTool), out devices, inputDevice);
					AddToolToDeviceData(toolData, devices);
					var vacuumTool = (VacuumTool)toolData.tool;
					vacuumTool.defaultOffset = WorkspaceModule.DefaultWorkspaceOffset;
					vacuumTool.defaultTilt = WorkspaceModule.DefaultWorkspaceTilt;
					vacuumTool.vacuumables = vacuumables.vacuumables;

					toolData = SpawnTool(typeof(MoveWorkspacesTool), out devices, inputDevice);
					AddToolToDeviceData(toolData, devices);

					toolData = SpawnTool(typeof(TransformTool), out devices, inputDevice);
					AddToolToDeviceData(toolData, devices);
					var transformTool = (TransformTool)toolData.tool;
					if (transformTool.IsSharedUpdater(transformTool))
						evr.m_DirectSelection.objectsGrabber = transformTool;

					toolData = SpawnTool(typeof(BlinkLocomotionTool), out devices, inputDevice);
					AddToolToDeviceData(toolData, devices);

					var evrMenus = evr.m_Menus;
					var mainMenu = evrMenus.SpawnMainMenu(typeof(MainMenu), inputDevice, false, out deviceData.mainMenuInput);
					deviceData.mainMenu = mainMenu;
					deviceData.menuHideFlags[mainMenu] = Menus.MenuHideFlags.Hidden;

					var pinnedTools = evr.m_PinnedTools;
					var mainMenuActivator = pinnedTools.SpawnMainMenuActivator(inputDevice);
					deviceData.mainMenuActivator = mainMenuActivator;
					mainMenuActivator.selected += pinnedTools.OnMainMenuActivatorSelected;
					mainMenuActivator.hoverStarted += pinnedTools.OnButtonHoverStarted;
					mainMenuActivator.hoverEnded += pinnedTools.OnButtonHoverEnded;

					var alternateMenu = evrMenus.SpawnAlternateMenu(typeof(RadialMenu), inputDevice, out deviceData.alternateMenuInput);
					deviceData.alternateMenu = alternateMenu;
					deviceData.menuHideFlags[alternateMenu] = Menus.MenuHideFlags.Hidden;
					alternateMenu.itemWasSelected += evrMenus.UpdateAlternateMenuOnSelectionChanged;

					var toolButtonActivePosition = new Vector3(0f, 0f, -0.035f); // Frontmost active button offset from the main menu activator
					PinnedToolButton.activePosition = toolButtonActivePosition; // Shared active button position
					var selectionToolButton = pinnedTools.SpawnPinnedToolButton(inputDevice);
					deviceData.pinnedToolButtons = new Dictionary<Type, PinnedToolButton>();
					selectionToolButton.toolType = typeof(SelectionTool); // Selection tool is visible & persistent by default
					deviceData.pinnedToolButtons.Add(selectionToolButton.toolType, selectionToolButton);
					selectionToolButton.order = 0; // The "active" tool occupies the zeroth position
					selectionToolButton.node = deviceData.node;
					selectionToolButton.highlightPinnedToolButtons = pinnedTools.HighlightPinnedToolButtons;
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

				if (usedDevices.Count == 0)
					usedDevices.Add(device);

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

			bool IsToolActive(Transform targetRayOrigin, Type toolType)
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
				var deviceInputModule = evr.m_DeviceInputModule;
				var pinnedTools = evr.m_PinnedTools;
				Rays.ForEachProxyDevice(deviceData =>
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

								if (!setSelectAsCurrentTool)
									pinnedTools.AddPinnedToolButton(deviceData, toolType);
							}
						}

						pinnedTools.SetupPinnedToolButtonsForDevice(deviceData, rayOrigin, toolType);
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
		}
	}
}
#endif
