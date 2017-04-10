#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class PinnedTools : Nested, IInterfaceConnector
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

			void AddPinnedToolButton(DeviceData deviceData, Type toolType)
			{
				var pinnedToolButtons = deviceData.pinnedToolButtons;
				if (pinnedToolButtons.ContainsKey(toolType)) // Return if tooltype already occupies a pinned tool button
					return;

				// Before adding new button, offset each button to a position greater than the zeroth/active tool position
				foreach (var pair in pinnedToolButtons)
				{
					pair.Value.order++;
				}

				var button = evr.m_Menus.SpawnPinnedToolButton(deviceData.inputDevice);
				pinnedToolButtons.Add(toolType, button);
				button.node = deviceData.node;
				button.toolType = toolType; // Assign Tool Type before assigning order
				button.order = 0; // Zeroth position is the active tool position
				button.DeletePinnedToolButton = DeletePinnedToolButton;
				button.highlightPinnedToolButtons = HighlightPinnedToolButtons;
			}

			void SetupPinnedToolButtonsForDevice(DeviceData deviceData, Transform rayOrigin, Type activeToolType)
			{
				Debug.LogError("<color=black>Setting up pinned tool button for type of : </color>" + activeToolType);
				const int kMaxButtonCount = 6;
				var order = 0;
				var buttons = deviceData.pinnedToolButtons;
				var buttonCount = buttons.Count;

				if (buttonCount >= kMaxButtonCount)
				{
					Debug.LogError("Attempting to add buttons beyond max count! Handle for removing highest ordered button and adding this new button!");
					return;
				}

				foreach (var pair in buttons)
				{
					var button = pair.Value;
					button.rayOrigin = rayOrigin;
					button.activeButtonCount = buttonCount; // Used to position buttons relative to count
					button.order = button.toolType == activeToolType ? 0 : ++order;

					if (button.order == 0)
						deviceData.proxy.HighlightDevice(deviceData.node, button.gradientPair); // Perform the higlight on the node with the button's gradient pair
				}
			}

			void DeletePinnedToolButton(Transform rayOrigin, PinnedToolButton buttonToDelete)
			{
				// Remove the pinned tool from the device data collection
				// re-order the current buttons
				// Highlight the device if the top/selected tool was the one that was closed

				Debug.LogError("<color=orange>DeletePinnedToolButton called</color>");

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


						Debug.LogError("Removing button : " + buttonToDelete.toolType);
						buttons.Remove(buttonToDelete.toolType);
						evr.m_Tools.SelectTool(rayOrigin, buttonToDelete.toolType);
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

			internal void HighlightPinnedToolButtons (Transform rayOrigin, bool enableHighlight)
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
		}
	}
}
#endif
