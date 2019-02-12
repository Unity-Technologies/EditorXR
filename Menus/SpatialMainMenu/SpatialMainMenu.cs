using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Tools;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class SpatialMainMenu : MonoBehaviour, IMainMenu, ISpatialMenuProvider, INodeToRay, ICreateWorkspace
    {
        readonly List<SpatialMenu.SpatialMenuData> m_SpatialMenuData = new List<SpatialMenu.SpatialMenuData>();

        readonly Bounds m_LocalBounds = new Bounds();

        public List<Type> menuTools { private get; set; }
        public List<Type> menuWorkspaces { private get; set; }
        public Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuProvider> settingsMenuProviders { get; set; }
        public Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuItemProvider> settingsMenuItemProviders { get; set; }
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
                    toolsSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer(itemName, description, (node) =>
                    {
                        this.SelectTool(this.RequestRayOriginFromNode(node), selectedType,
                            hideMenu: typeof(IInstantiateMenuUI).IsAssignableFrom(selectedType));
                    }));

                if (isWorkspace)
                    workspaceSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer(itemName, description,
                        (node) => this.CreateWorkspace(selectedType)));
            }

            spatialMenuData.Add(new SpatialMenu.SpatialMenuData("Workspaces", "Open a workspace", workspaceSpatialMenuElements));
            spatialMenuData.Add(new SpatialMenu.SpatialMenuData("Tools", "Select a tool", toolsSpatialMenuElements));

            toolsSpatialMenuElements.Add(new SpatialMenu.SpatialMenuElementContainer("Selection Tool", "Perform standard object selection & manipulation", (node) =>
            {
                this.SelectTool(this.RequestRayOriginFromNode(node), typeof(SelectionTool), hideMenu: true);
            }));
        }

        public void AddSettingsMenu(ISettingsMenuProvider provider)
        {
        }

        public void RemoveSettingsMenu(ISettingsMenuProvider provider)
        {
        }

        public void AddSettingsMenuItem(ISettingsMenuItemProvider provider)
        {
        }

        public void RemoveSettingsMenuItem(ISettingsMenuItemProvider provider)
        {
        }
    }
}
