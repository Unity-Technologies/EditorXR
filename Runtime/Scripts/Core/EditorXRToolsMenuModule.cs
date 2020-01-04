using System;
using System.Linq;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Core
{
    class EditorXRToolsMenuModule : IModuleDependency<EditorXRRayModule>,
        IModuleDependency<EditorXRToolModule>, IModuleDependency<EditorXRMenuModule>, IProvidesPreviewInToolMenuButton
    {
        EditorXRRayModule m_RayModule;
        EditorXRToolModule m_ToolModule;
        EditorXRMenuModule m_MenuModule;

        public void ConnectDependency(EditorXRRayModule dependency)
        {
            m_RayModule = dependency;
        }

        public void ConnectDependency(EditorXRToolModule dependency)
        {
            m_ToolModule = dependency;
        }

        public void ConnectDependency(EditorXRMenuModule dependency)
        {
            m_MenuModule = dependency;
        }

        public void LoadModule()
        {
            ToolsMenuMethods.mainMenuActivatorSelected = OnMainMenuActivatorSelected;
            ToolsMenuMethods.selectTool = OnToolButtonClicked;
        }

        public void UnloadModule() { }

        public void PreviewInToolsMenuButton(Transform rayOrigin, Type toolType, string toolDescription)
        {
            // Prevents menu buttons of types other than ITool from triggering any ToolMenuButton preview actions
            if (!toolType.GetInterfaces().Contains(typeof(ITool)))
                return;

            m_RayModule.ForEachProxyDevice(deviceData =>
            {
                if (deviceData.rayOrigin == rayOrigin) // Enable Tools Menu preview on the opposite (handed) device
                {
                    var previewToolMenuButton = deviceData.toolsMenu.PreviewToolsMenuButton;
                    previewToolMenuButton.previewToolType = toolType;
                    previewToolMenuButton.previewToolDescription = toolDescription;
                }
            });
        }

        public void ClearToolsMenuButtonPreview()
        {
            m_RayModule.ForEachProxyDevice(deviceData => { deviceData.toolsMenu.PreviewToolsMenuButton.previewToolType = null; });
        }

        void OnToolButtonClicked(Transform rayOrigin, Type toolType)
        {
            if (toolType == typeof(IMainMenu))
                OnMainMenuActivatorSelected(rayOrigin);
            else
                m_ToolModule.SelectTool(rayOrigin, toolType);
        }

        void OnMainMenuActivatorSelected(Transform rayOrigin)
        {
            var targetToolRayOrigin = m_ToolModule.deviceData.FirstOrDefault(data => data.rayOrigin != rayOrigin).rayOrigin;
            m_MenuModule.OnMainMenuActivatorSelected(rayOrigin, targetToolRayOrigin);
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var previewInToolsMenuSubscriber = obj as IFunctionalitySubscriber<IProvidesPreviewInToolMenuButton>;
            if (previewInToolsMenuSubscriber != null)
                previewInToolsMenuSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
