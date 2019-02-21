using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class ToolsMenu : Nested
        {
            public ToolsMenu()
            {
                IToolsMenuMethods.mainMenuActivatorSelected = OnMainMenuActivatorSelected;
                IToolsMenuMethods.selectTool = OnToolButtonClicked;

                IPreviewInToolMenuButtonMethods.previewInToolMenuButton = PreviewToolInToolMenuButton;
                IPreviewInToolMenuButtonMethods.clearToolMenuButtonPreview = ClearToolMenuButtonPreview;
            }

            static void PreviewToolInToolMenuButton(Transform rayOrigin, Type toolType, string toolDescription)
            {
                // Prevents menu buttons of types other than ITool from triggering any ToolMenuButton preview actions
                if (!toolType.GetInterfaces().Contains(typeof(ITool)))
                    return;

                Rays.ForEachProxyDevice((deviceData) =>
                {
                    if (deviceData.rayOrigin == rayOrigin) // Enable Tools Menu preview on the opposite (handed) device
                    {
                        var previewToolMenuButton = deviceData.toolsMenu.PreviewToolsMenuButton;
                        previewToolMenuButton.previewToolType = toolType;
                        previewToolMenuButton.previewToolDescription = toolDescription;
                    }
                });
            }

            static void ClearToolMenuButtonPreview()
            {
                Rays.ForEachProxyDevice(deviceData => { deviceData.toolsMenu.PreviewToolsMenuButton.previewToolType = null; });
            }

            static void OnToolButtonClicked(Transform rayOrigin, Type toolType)
            {
                if (toolType == typeof(IMainMenu))
                    OnMainMenuActivatorSelected(rayOrigin);
                else
                    evr.GetNestedModule<Tools>().SelectTool(rayOrigin, toolType);
            }

            static void OnMainMenuActivatorSelected(Transform rayOrigin)
            {
                var targetToolRayOrigin = evr.m_DeviceData.FirstOrDefault(data => data.rayOrigin != rayOrigin).rayOrigin;
                Menus.OnMainMenuActivatorSelected(rayOrigin, targetToolRayOrigin);
            }
        }
    }
}
