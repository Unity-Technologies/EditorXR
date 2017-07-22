#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class PinnedToolButtons : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var mainMenu = obj as IMainMenu;
				if (mainMenu != null)
					mainMenu.previewToolInPinnedToolButton = PreviewToolInPinnedToolButton;
			}

			public void DisconnectInterface(object obj, Transform rayOrigin = null)
			{
			}

/* TODO remove after removal of main menu activator codebase
			internal MainMenuActivator SpawnMainMenuActivator(InputDevice device)
			{
				var mainMenuActivator = ObjectUtils.Instantiate(evr.m_MainMenuActivatorPrefab.gameObject).GetComponent<MainMenuActivator>();
				evr.m_Interfaces.ConnectInterfaces(mainMenuActivator, device);

				return mainMenuActivator;
			}
			internal IPinnedToolButton SpawnPinnedToolButton(InputDevice device)
			{
				var button = ObjectUtils.Instantiate(evr.m_PinnedToolButtonPrefab.gameObject).GetComponent<IPinnedToolButton>();
				evr.m_Interfaces.ConnectInterfaces(button, device);

				return button;
			}
*/

			/*
			internal IPinnedToolButton AddPinnedToolButton(DeviceData deviceData, Type toolType, Sprite buttonIcon)
			{
				Debug.LogWarning("<color=green>SPAWNING pinned tool button for type of : </color>" + toolType);
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

				// Initialize button in alternate position if the alternate menu is hidden
				IPinnedToolButton mainMenu = null;
				if (toolType == typeof(IMainMenu))
					mainMenu = button;
				else
					pinnedToolButtons.TryGetValue(typeof(IMainMenu), out mainMenu);

				button.moveToAlternatePosition = mainMenu != null && mainMenu.moveToAlternatePosition;
				button.node = deviceData.node;
				button.toolType = toolType; // Assign Tool Type before assigning order
				button.icon = buttonIcon;
				//button.order = button.activeToolOrderPosition; // first position is the active tool position
				button.deletePinnedToolButton = DeletePinnedToolButton;
				button.revealAllToolButtons = RevealAllToolButtons;
				button.selectTool = ToolButtonClicked;
				//button.highli = HighlightSingleButtonWithoutMenu;
				button.selectHighlightedButton = SelectHighlightedButton;
				button.deleteHighlightedButton = DeleteHighlightedButton;
				//button.selected += OnMainMenuActivatorSelected;
				button.hoverEnter += onButtonHoverEnter;
				button.hoverExit += onButtonHoverExit;

				return button;
			}
*/
/*
			internal void SetupPinnedToolButtonsForDevice(DeviceData deviceData, Transform rayOrigin, Type activeToolType)
			{
				Debug.LogError("<color=black>Setting up pinned tool button for type of : </color>" + activeToolType);
				activeToolType = activeToolType == typeof(IMainMenu) ? typeof(SelectionTool) : activeToolType; // Assign SelectionTool if setting up for IMainMenu
				const int kMaxButtonCount = 16;
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
						Debug.LogWarning("Setting up main menu button");
						button.order = button.menuButtonOrderPosition;
						button.revealed = true;
					}
					else
					{
						button.order = button.toolType == activeToolType ? button.activeToolOrderPosition : ++inactiveButtonInitialOrderPosition;
						Debug.LogWarning("Setting up button : " + button.toolType + " - ORDER : " + button.order);
					}

					if (button.order == button.activeToolOrderPosition)
						deviceData.proxy.HighlightDevice(deviceData.node, button.gradientPair); // Perform the higlight on the node with the button's gradient pair
				}
			}
*/

			internal void HighlightDevice(Transform rayOrigin, GradientPair gradientPair)
			{
				Rays.ForEachProxyDevice(deviceData =>
				{
					if (deviceData.rayOrigin == rayOrigin)
					{
						deviceData.proxy.HighlightDevice(deviceData.node, gradientPair);
					}
				});
			}

			void DeletePinnedToolButton(Transform rayOrigin, IPinnedToolButton buttonToDelete)
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
						/*
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
						Tools.SelectTool(rayOrigin, selectedButtontype);
						// TODO remove after refacter : SetupPinnedToolButtonsForDevice(deviceData, rayOrigin, selectedButtontype);
						*/
					}
				});
			}

			internal IPinnedToolButton PreviewToolInPinnedToolButton (Transform rayOrigin, Type toolType)
			{
				// Prevents menu buttons of types other than ITool from triggering any pinned tool button preview actions
				if (!toolType.GetInterfaces().Contains(typeof(ITool)))
					return null;

				IPinnedToolButton previewPinnedToolButton = null;
				Rays.ForEachProxyDevice((deviceData) =>
				{
					if (deviceData.rayOrigin == rayOrigin) // enable pinned tool preview on the opposite (handed) device
					{
						var pinnedToolsMenu = deviceData.pinnedToolsMenu;
						previewPinnedToolButton = pinnedToolsMenu.previewToolButton;
						previewPinnedToolButton.previewToolType = toolType;
					}
				});

				return previewPinnedToolButton;
			}

			internal void ToolButtonClicked(Transform rayOrigin, Type toolType)
			{
				Debug.LogError("<color=green>TOOL BUTTON CLICKED : </color>" + toolType.ToString());

				if (toolType == typeof(IMainMenu))
					OnMainMenuActivatorSelected(rayOrigin);
				else
					Tools.SelectTool(rayOrigin, toolType);
			}

			/*
			internal void RevealAllToolButtons (Transform rayOrigin, bool reveal)
			{
				Rays.ForEachProxyDevice(deviceData =>
				{
					if (deviceData.rayOrigin == rayOrigin)
					{
						var buttons = deviceData.pinnedToolButtons;
						foreach (var pair in buttons)
						{
							pair.Value.revealed = reveal;
						}
					}
				});
			}
			*/

			/*
			// TODO: move into pinned tool button controller?
			internal void HighlightSingleButtonWithoutMenu (Transform rayOrigin, int buttonOrderPosition, bool highlight = true)
			{
				Rays.ForEachProxyDevice(deviceData =>
				{
					if (deviceData.rayOrigin == rayOrigin)
					{
						var buttons = deviceData.pinnedToolButtons;
						foreach (var pair in buttons)
						{
							var toolButton = pair.Value;
							toolButton.highlighted = toolButton.order == buttonOrderPosition ? highlight : false;
						}
					}
				});
			}
			*/

			// TODO: move into pinned tool button controller
			internal void DeleteHighlightedButton (Transform rayOrigin)
			{
				Rays.ForEachProxyDevice(deviceData =>
				{
					if (deviceData.rayOrigin == rayOrigin)
					{
						Debug.LogError("RE IMPLEMENT DELETING OF TOOL BUTTONS!!! after refactor");
						/*
						var buttons = deviceData.pinnedToolButtons;
						foreach (var pair in buttons)
						{
							var toolButton = pair.Value;
							if (toolButton.highlighted == true)
							{
								DeletePinnedToolButton(rayOrigin, toolButton);
								return;
							}
						}
						*/
					}
				});
			}

			internal void SelectHighlightedButton (Transform rayOrigin)
			{
				Debug.LogError("SELECT HIGHLIGHTED BUTTON CALLED - ADD FUNCTIOALITY BACK IN!!!");
				/*
				Rays.ForEachProxyDevice(deviceData =>
				{
					if (deviceData.rayOrigin == rayOrigin)
					{
						var buttons = deviceData.pinnedToolButtons;
						foreach (var pair in buttons)
						{
							var toolButton = pair.Value;
							if (toolButton.highlighted)
							{
								ToolButtonClicked(toolButton.rayOrigin, toolButton.toolType);
								break;
							}
						}
					}
				});
				*/
			}

			/*
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
			*/

			internal void OnMainMenuActivatorSelected(Transform rayOrigin)
			{
				Debug.LogError("OnMainMenuActivatorSelected called!");
				var targetToolRayOrigin = evr.m_DeviceData.FirstOrDefault(data => data.rayOrigin != rayOrigin).rayOrigin;
				var deviceData = evr.m_DeviceData.FirstOrDefault(data => data.rayOrigin == rayOrigin);

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
