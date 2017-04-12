#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class ToolButtons : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var mainMenu = obj as IMainMenu;
				if (mainMenu != null)
					mainMenu.previewToolInPinnedToolButton = PreviewToolInPinnedToolButton;
			}

			public void DisconnectInterface(object obj)
			{
			}
/* TODO remove after removal of main menu activator codebase
			internal MainMenuActivator SpawnMainMenuActivator(InputDevice device)
			{
				var mainMenuActivator = ObjectUtils.Instantiate(evr.m_MainMenuActivatorPrefab.gameObject).GetComponent<MainMenuActivator>();
				evr.m_Interfaces.ConnectInterfaces(mainMenuActivator, device);

				return mainMenuActivator;
			}
*/
			internal PinnedToolButton SpawnPinnedToolButton(InputDevice device)
			{
				var button = ObjectUtils.Instantiate(evr.m_PinnedToolButtonPrefab.gameObject).GetComponent<PinnedToolButton>();
				evr.m_Interfaces.ConnectInterfaces(button, device);

				return button;
			}

			internal PinnedToolButton AddPinnedToolButton(DeviceData deviceData, Type toolType)
			{
				Debug.LogError("<color=green>SPAWNING pinned tool button for type of : </color>" + toolType);
				var pinnedToolButtons = deviceData.pinnedToolButtons;
				if (pinnedToolButtons.ContainsKey(toolType)) // Return if tooltype already occupies a pinned tool button
					return null;

				// Before adding new button, offset each button to a position greater than the zeroth/active tool position
				foreach (var pair in pinnedToolButtons)
				{
					if (pair.Value.order != pair.Value.menuButtonOrderPosition) // don't move the main menu button
						pair.Value.order++;
				}

				var button = SpawnPinnedToolButton(deviceData.inputDevice);
				pinnedToolButtons.Add(toolType, button);
				button.node = deviceData.node;
				button.toolType = toolType; // Assign Tool Type before assigning order
				//button.order = button.activeToolOrderPosition; // first position is the active tool position
				button.deletePinnedToolButton = DeletePinnedToolButton;
				button.highlightAllToolButtons = HighlightAllToolButtons;
				button.selectTool = ToolButtonClicked;
				//button.selected += OnMainMenuActivatorSelected;
				button.hoverEnter += OnButtonHoverEnter;
				button.hoverExit += OnButtonHoverExit;

				return button;
			}

			internal void SetupPinnedToolButtonsForDevice(DeviceData deviceData, Transform rayOrigin, Type activeToolType)
			{
				Debug.LogError("<color=black>Setting up pinned tool button for type of : </color>" + activeToolType);
				activeToolType = activeToolType == typeof(IMainMenu) ? typeof(SelectionTool) : activeToolType; // Assign SelectionTool if setting up for IMainMenu
				const int kMaxButtonCount = 6;
				var buttons = deviceData.pinnedToolButtons;
				var inactiveButtonInitialOrderPosition = -1;
				var buttonCount = buttons.Count; // Position buttons relative to count

				if (buttonCount >= kMaxButtonCount)
				{
					Debug.LogError("Attempting to add buttons beyond max count! Handle for removing highest ordered button and adding this new button!");
					return;
				}

				foreach (var pair in buttons)
				{
					var button = pair.Value;
					inactiveButtonInitialOrderPosition = inactiveButtonInitialOrderPosition  == -1 ? button.activeToolOrderPosition : inactiveButtonInitialOrderPosition;
					button.rayOrigin = rayOrigin;
					button.activeButtonCount = buttonCount;

					if (button.toolType == typeof(IMainMenu))
					{
						Debug.LogError("Setting up main menu button");
						button.order = button.menuButtonOrderPosition;
					}
					else
					{
						button.order = button.toolType == activeToolType ? button.activeToolOrderPosition : ++inactiveButtonInitialOrderPosition;
						Debug.LogError("Setting up button : " + button.toolType + " - ORDER : " + button.order);
					}

					if (button.order == 0)
						deviceData.proxy.HighlightDevice(deviceData.node, button.gradientPair); // Perform the higlight on the node with the button's gradient pair
				}
			}

			void DeletePinnedToolButton(Transform rayOrigin, PinnedToolButton buttonToDelete)
			{
				// Remove the pinned tool from the device data collection
				// re-order the current buttons
				// Highlight the device if the top/selected tool was the one that was closed

				Debug.LogError("<color=orange>deletePinnedToolButton called</color>");

				//var result = false;
				//var deviceInputModule = evr.m_DeviceInputModule;
				Type selectedButtontype = null;
				Rays.ForEachProxyDevice(deviceData =>
				{
					if (deviceData.rayOrigin == rayOrigin)
					{
						var buttons = deviceData.pinnedToolButtons;
						var selectedButtonOrder = buttons.Count;
						foreach (var pair in deviceData.pinnedToolButtons)
						{
							var button = pair.Value;
							if (button != buttonToDelete)
							{
								// Identify the new selected button
								selectedButtonOrder = button.order < selectedButtonOrder ? button.order : selectedButtonOrder;
								selectedButtontype = selectedButtonOrder == button.order ? button.toolType : selectedButtontype;
							}
						}


						Debug.LogError("Removing button : " + buttonToDelete.toolType + " - Setting new active button of type : " + selectedButtontype);
						buttons.Remove(buttonToDelete.toolType);
						evr.m_Tools.SelectTool(rayOrigin, selectedButtontype);
						SetupPinnedToolButtonsForDevice(deviceData, rayOrigin, selectedButtontype);
					}
				});
			}

			internal PinnedToolButton PreviewToolInPinnedToolButton (Transform rayOrigin, Type toolType)
			{
				// Prevents menu buttons of types other than ITool from triggering any pinned tool button preview actions
				if (!toolType.GetInterfaces().Contains(typeof(ITool)))
					return null;

				PinnedToolButton pinnedToolButton = null;
				Rays.ForEachProxyDevice((deviceData) =>
				{
					if (deviceData.rayOrigin == rayOrigin) // enable pinned tool preview on the opposite (handed) device
					{
						var pinnedToolButtons = deviceData.pinnedToolButtons;
						foreach (var pair in pinnedToolButtons)
						{
							var button = pair.Value;
							if (button.order == 0)
							{
								pinnedToolButton = button;
								pinnedToolButton.previewToolType = toolType;
								break;
							}
						}
					}
				});

				return pinnedToolButton;
			}

			internal void ToolButtonClicked(Transform rayOrigin, Type toolType)
			{
				if (toolType == typeof(IMainMenu))
					OnMainMenuActivatorSelected(rayOrigin);
				else
					evr.m_Tools.SelectTool(rayOrigin, toolType);
			}

			internal void HighlightAllToolButtons (Transform rayOrigin, bool enableHighlight)
			{
				Rays.ForEachProxyDevice(deviceData =>
				{
					if (deviceData.rayOrigin == rayOrigin)
					{
						var buttons = deviceData.pinnedToolButtons;
						foreach (var pair in buttons)
						{
							pair.Value.highlighted = enableHighlight;
						}
					}
				});
			}

			internal void OnButtonHoverEnter(Transform rayOrigin)
			{
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
				{
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] |= Menus.MenuHideFlags.OverActivator;
					}
				}
			}

			internal void OnButtonHoverExit(Transform rayOrigin)
			{
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
				{
					var menus = new List<IMenu>(deviceData.menuHideFlags.Keys);
					foreach (var menu in menus)
					{
						deviceData.menuHideFlags[menu] &= ~Menus.MenuHideFlags.OverActivator;
					}
				}
			}

			internal void OnMainMenuActivatorSelected(Transform rayOrigin)
			{
				Debug.LogError("OnMainMenuActivatorSelected called!");
				var targetToolRayOrigin = evr.m_DeviceData.FirstOrDefault(data => data.rayOrigin != rayOrigin).rayOrigin;
				var deviceData = evr.m_DeviceData.FirstOrDefault(data => data.rayOrigin == rayOrigin);
				if (targetToolRayOrigin == null)
					Debug.LogError("<color=red>????????????????????????????????????????????????????????????</color>");

				foreach (var origin in deviceData.proxy.rayOrigins.Values)
				{
					targetToolRayOrigin = origin != rayOrigin ? origin : null; // Assign the opposite hand's rayOrigin
				}

				if (deviceData != null)
				{
					var mainMenu = deviceData.mainMenu;
					if (mainMenu != null)
					{
						var menuHideFlags = deviceData.menuHideFlags;
						menuHideFlags[mainMenu] ^= Menus.MenuHideFlags.Hidden;

						var customMenu = deviceData.customMenu;
						if (customMenu != null)
							menuHideFlags[customMenu] &= ~Menus.MenuHideFlags.Hidden;

						mainMenu.targetRayOrigin = targetToolRayOrigin;
					}
				}
			}
		}
	}
}
#endif
