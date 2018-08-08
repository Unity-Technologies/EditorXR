#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class Tools Menu functionality
    /// </summary>
    public interface IToolsMenu : IUsesMenuOrigins, ICustomActionMap, IUsesNode, ISelectTool
    {
        /// <summary>
        /// Bool denoting that the alternate menu (radial menu, etc) is currently visible
        /// Allows the ToolsMenu to adapt visibility state changes that occur  in the AlternateMenu
        /// </summary>
        bool alternateMenuVisible { set; }

        /// <summary>
        /// This menu's RayOrigin
        /// </summary>
        Transform rayOrigin { get; set; }

        /// <summary>
        /// The ToolsMenuButton that the menu uses to display tool previews
        /// </summary>
        IToolsMenuButton PreviewToolsMenuButton { get; }

        /// <summary>
        /// Function that assigns & sets up a tool button for a given tool type
        /// This method isn't hooked up in EVR, it should reside in the implementing class
        /// Type of tool; tool icon sprite (if available), tool description
        /// </summary>
        Action<Type, Sprite, String> setButtonForType { get; }

        /// <summary>
        /// Delete the tool button with corresponding type of the first parameter.
        /// Then, select the tool button with corresponds to the type of the second parameter.
        /// </summary>
        Action<Type, Type> deleteToolsMenuButton { get; }

        /// <summary>
        /// Set the interactable state on the main menu activator button
        /// </summary>
        bool mainMenuActivatorInteractable { set; }
    }

    public static class IToolsMenuMethods
    {
        public static Action<Transform> mainMenuActivatorSelected { get; set; }
        public static Action<Transform, Type> selectTool { get; set; }

        /// <summary>
        /// Called when selecting the main menu activator
        /// </summary>
        /// <param name="rayOrigin">This menu's RayOrigin</param>
        public static void MainMenuActivatorSelected(this IToolsMenu obj, Transform rayOrigin)
        {
            mainMenuActivatorSelected(rayOrigin);
        }

        /// <summary>
        /// Selects a tool, based on type, from a Tools Menu Button
        /// </summary>
        /// <param name="rayOrigin">This menu's RayOrigin</param>
        /// <param name="type">The type of the tool that is to be selected</param>
        public static void SelectTool(this IToolsMenu obj, Transform rayOrigin, Type type)
        {
            selectTool(rayOrigin, type);
        }
    }
}
#endif
