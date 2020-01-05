using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.EditorXR.Core;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Tools;
using Unity.Labs.EditorXR.Workspaces;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Menus
{
    sealed class SpatialMainMenu : MonoBehaviour, IMainMenu, ISpatialMenuProvider, INodeToRay, IUsesCreateWorkspace
    {
        readonly List<SpatialMenu.SpatialMenuData> m_SpatialMenuData = new List<SpatialMenu.SpatialMenuData>();

        readonly Bounds m_LocalBounds = new Bounds();

        public List<Type> menuTools { private get; set; }
        public List<Type> menuWorkspaces { private get; set; }
        public Dictionary<Tuple<Type, Transform>, ISettingsMenuProvider> settingsMenuProviders { get; set; }
        public Dictionary<Tuple<Type, Transform>, ISettingsMenuItemProvider> settingsMenuItemProviders { get; set; }
        public Transform targetRayOrigin { private get; set; }
        public Node node { get; set; }

        public GameObject menuContent { get { return null; } }

        public Transform rayOrigin { private get; set; }

        public Bounds localBounds { get { return m_LocalBounds; } }
        public int priority { get { return 0; } }

        public bool focus { get { return false; } }

        public List<SpatialMenu.SpatialMenuData> spatialMenuData { get { return m_SpatialMenuData; } }

        public MenuHideFlags menuHideFlags
        {
            get { return MenuHideFlags.Hidden; }
            set { }
        }

#if !FI_AUTOFILL
        IProvidesSelectTool IFunctionalitySubscriber<IProvidesSelectTool>.provider { get; set; }
        IProvidesPreviewInToolMenuButton IFunctionalitySubscriber<IProvidesPreviewInToolMenuButton>.provider { get; set; }
        IProvidesCreateWorkspace IFunctionalitySubscriber<IProvidesCreateWorkspace>.provider { get; set; }
#endif

        void Start()
        {
            Populate();
        }

        void Populate()
        {
            var workspaceSpatialMenuElements = new List<SpatialMenu.SpatialMenuElementContainer>();
            var toolsSpatialMenuElements = new List<SpatialMenu.SpatialMenuElementContainer>();

            var types = new HashSet<Type>();
            types.UnionWith(menuTools);
            types.UnionWith(menuWorkspaces);

            foreach (var type in types)
            {
                var customMenuAttribute = (MainMenuItemAttribute)type.GetCustomAttributes(typeof(MainMenuItemAttribute), false).FirstOrDefault();
                if (customMenuAttribute != null && !customMenuAttribute.shown)
                    continue;

                var isTool = typeof(ITool).IsAssignableFrom(type) && menuTools.Contains(type);
                var isWorkspace = typeof(Workspace).IsAssignableFrom(type);

                var selectedType = type; // Local variable for closure

                var itemName = type.Name;
                var description = string.Empty;

                if (customMenuAttribute != null && customMenuAttribute.shown)
                {
                    itemName = customMenuAttribute.name;
                    description = customMenuAttribute.description;
                }

                if (isTool)
                    toolsSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer(itemName, description, correspondingNode =>
                    {
                        this.SelectTool(this.RequestRayOriginFromNode(correspondingNode), selectedType,
                            hideMenu: typeof(IUsesInstantiateMenuUI).IsAssignableFrom(selectedType));
                    }));

                if (isWorkspace)
                    workspaceSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer(itemName, description,
                        correspondingNode => this.CreateWorkspace(selectedType)));
            }

            spatialMenuData.Add(new SpatialMenu.SpatialMenuData("Workspaces", "Open a workspace", workspaceSpatialMenuElements));
            spatialMenuData.Add(new SpatialMenu.SpatialMenuData("Tools", "Select a tool", toolsSpatialMenuElements));

            toolsSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer("Selection Tool", "Perform standard object selection & manipulation", correspondingNode =>
            {
                this.SelectTool(this.RequestRayOriginFromNode(correspondingNode), typeof(SelectionTool), hideMenu: true);
            }));
        }

        public void AddSettingsMenu(ISettingsMenuProvider menuProvider)
        {
        }

        public void RemoveSettingsMenu(ISettingsMenuProvider menuProvider)
        {
        }

        public void AddSettingsMenuItem(ISettingsMenuItemProvider menuProvider)
        {
        }

        public void RemoveSettingsMenuItem(ISettingsMenuItemProvider menuProvider)
        {
        }
    }
}
