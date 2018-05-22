
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// The main menu that can be shown on device proxies
    /// </summary>
    public interface IMainMenu : IMenu, ISelectTool, IPreviewInToolsMenuButton
    {
        /// <summary>
        /// The menu tools that will populate the menu
        /// </summary>
        List<Type> menuTools { set; }

        /// <summary>
        /// The workspaces that are selectable from the menu
        /// </summary>
        List<Type> menuWorkspaces { set; }

        /// <summary>
        /// The types which provide a settings menu
        /// </summary>
        Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuProvider> settingsMenuProviders { set; }

        /// <summary>
        /// The types which provide a settings menu item
        /// </summary>
        Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuItemProvider> settingsMenuItemProviders { set; }

        /// <summary>
        /// The ray origin that spawned the menu and will be used for node-specific operations (e.g. selecting a tool)
        /// </summary>
        Transform targetRayOrigin { set; }

        /// <summary>
        /// Does this menu have focus?
        /// </summary>
        bool focus { get; }

        /// <summary>
        /// Add a settings menu to this menu
        /// </summary>
        /// <param name="provider">The object providing the settings menu</param>
        void AddSettingsMenu(ISettingsMenuProvider provider);

        /// <summary>
        /// Remove a settings menu from this menu
        /// </summary>
        /// <param name="provider">The object which provided the settings menu</param>
        void RemoveSettingsMenu(ISettingsMenuProvider provider);

        /// <summary>
        /// Add a settings menu item to this menu
        /// </summary>
        /// <param name="provider">The object providing the settings menu item</param>
        void AddSettingsMenuItem(ISettingsMenuItemProvider provider);

        /// <summary>
        /// Remove a settings menu item from this menu
        /// </summary>
        /// <param name="provider">The object which provided the settings menu item</param>
        void RemoveSettingsMenuItem(ISettingsMenuItemProvider provider);
    }
}

