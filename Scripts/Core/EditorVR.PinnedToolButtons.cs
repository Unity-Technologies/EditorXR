#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class PinnedToolButtons : Nested, IInterfaceConnector
		{
			public PinnedToolButtons()
			{
				IPinnedToolsMenuMethods.highlightDevice = HighlightDevice;
				IPinnedToolsMenuMethods.mainMenuActivatorSelected = OnMainMenuActivatorSelected;
				IPinnedToolsMenuMethods.selectTool = OnToolButtonClicked;
			}

			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var mainMenu = obj as IMainMenu;
				if (mainMenu != null)
					mainMenu.previewToolInPinnedToolButton = PreviewToolInPinnedToolButton;
			}

			public void DisconnectInterface(object obj, Transform rayOrigin = null)
			{
			}

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

			internal IPinnedToolButton PreviewToolInPinnedToolButton (Transform rayOrigin, Type toolType, string toolDescription)
			{
				// Prevents menu buttons of types other than ITool from triggering any pinned tool button preview actions
				if (!toolType.GetInterfaces().Contains(typeof(ITool)))
					return null;

				IPinnedToolButton previewPinnedToolButton = null;
				Rays.ForEachProxyDevice((deviceData) =>
				{
					if (deviceData.rayOrigin == rayOrigin) // Enable pinned tool preview on the opposite (handed) device
					{
						var pinnedToolsMenu = deviceData.pinnedToolsMenu;
						previewPinnedToolButton = pinnedToolsMenu.previewToolButton;
						previewPinnedToolButton.previewToolType = toolType;
						previewPinnedToolButton.previewToolDescription = toolDescription;
					}
				});

				return previewPinnedToolButton;
			}

			internal void OnToolButtonClicked(Transform rayOrigin, Type toolType)
			{
				if (toolType == typeof(IMainMenu))
					OnMainMenuActivatorSelected(rayOrigin);
				else
					Tools.SelectTool(rayOrigin, toolType);
			}

			internal void OnMainMenuActivatorSelected(Transform rayOrigin)
			{
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
