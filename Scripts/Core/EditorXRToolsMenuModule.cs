#if UNITY_2018_3_OR_NEWER
using System;
using System.Linq;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    class EditorXRToolsMenuModule : IModuleDependency<EditorVR>, IModuleDependency<EditorXRRayModule>,
        IModuleDependency<EditorXRToolModule>, IModuleDependency<EditorXRMenuModule>
    {
        EditorVR m_EditorVR;
        EditorXRRayModule m_RayModule;
        EditorXRToolModule m_ToolModule;
        EditorXRMenuModule m_MenuModule;

        public void ConnectDependency(EditorVR dependency)
        {
            m_EditorVR = dependency;
        }

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
            IToolsMenuMethods.mainMenuActivatorSelected = OnMainMenuActivatorSelected;
            IToolsMenuMethods.selectTool = OnToolButtonClicked;

            IPreviewInToolMenuButtonMethods.previewInToolMenuButton = PreviewToolInToolMenuButton;
            IPreviewInToolMenuButtonMethods.clearToolMenuButtonPreview = ClearToolMenuButtonPreview;
        }

        public void UnloadModule() { }

        void PreviewToolInToolMenuButton(Transform rayOrigin, Type toolType, string toolDescription)
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

        void ClearToolMenuButtonPreview()
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
            var targetToolRayOrigin = m_EditorVR.deviceData.FirstOrDefault(data => data.rayOrigin != rayOrigin).rayOrigin;
            m_MenuModule.OnMainMenuActivatorSelected(rayOrigin, targetToolRayOrigin);
        }
    }
}
#endif
