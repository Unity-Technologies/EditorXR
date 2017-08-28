#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Menus;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class ToolMenu : Nested
		{
			public ToolMenu()
			{
				IPinnedToolsMenuMethods.mainMenuActivatorSelected = OnMainMenuActivatorSelected;
				IPinnedToolsMenuMethods.selectTool = OnToolButtonClicked;

				IPreviewInToolMenuButtonMethods.previewInToolMenuButton = PreviewToolInToolMenuButton;
				IPreviewInToolMenuButtonMethods.clearToolMenuButtonPreview = ClearToolMenuButtonPreview;
			}

			static void PreviewToolInToolMenuButton (Transform rayOrigin, Type toolType, string toolDescription)
			{
				// Prevents menu buttons of types other than ITool from triggering any ToolMenuButton preview actions
				if (!toolType.GetInterfaces().Contains(typeof(ITool)))
					return;

				Rays.ForEachProxyDevice((deviceData) =>
				{
					if (deviceData.rayOrigin == rayOrigin) // Enable pinned tool preview on the opposite (handed) device
					{
						var previewToolMenuButton = deviceData.ToolMenu.previewToolButton;
						previewToolMenuButton.previewToolType = toolType;
						previewToolMenuButton.previewToolDescription = toolDescription;
					}
				});
			}

			static void ClearToolMenuButtonPreview()
			{
				Rays.ForEachProxyDevice((deviceData) =>
				{
					deviceData.ToolMenu.previewToolButton.previewToolType = null;
				});
			}

			static void OnToolButtonClicked(Transform rayOrigin, Type toolType)
			{
				if (toolType == typeof(IMainMenu))
					OnMainMenuActivatorSelected(rayOrigin);
				else
					Tools.SelectTool(rayOrigin, toolType);
			}

			static void OnMainMenuActivatorSelected(Transform rayOrigin)
			{
				var targetToolRayOrigin = evr.m_DeviceData.FirstOrDefault(data => data.rayOrigin != rayOrigin).rayOrigin;
				var deviceData = evr.m_DeviceData.FirstOrDefault(data => data.rayOrigin == rayOrigin);

				if (deviceData != null)
				{
					var mainMenu = deviceData.mainMenu;
					if (mainMenu != null)
					{
						var menuHideFlags = deviceData.menuHideData;
						menuHideFlags[mainMenu].hideFlags ^= MenuHideFlags.Hidden;

						var customMenu = deviceData.customMenu;
						if (customMenu != null)
							menuHideFlags[customMenu].hideFlags &= ~MenuHideFlags.Hidden;

						mainMenu.targetRayOrigin = targetToolRayOrigin;
					}
				}
			}
		}
	}
}
#endif
