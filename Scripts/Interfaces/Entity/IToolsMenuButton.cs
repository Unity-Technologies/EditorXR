using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides an interface for handling Tools Menu Button functionality
    /// </summary>
    public interface IToolsMenuButton
    {
        /// <summary>
        /// The type to preview in the button temporarily
        /// </summary>
        Type previewToolType { set; }

        /// <summary>
        /// The toolType assigned to this button
        /// </summary>
        Type toolType { get; }

        /// <summary>
        /// The order of this button
        /// Position/rotation may change according to order
        /// </summary>
        int order { get; set; }

        /// <summary>
        /// The maximum number of buttons that can be displayed by a given ToolsMenu
        /// </summary>
        int maxButtonCount { set; }

        /// <summary>
        /// The z positional offset to apply when button if highlighted
        /// </summary>
        float iconHighlightedLocalZOffset { set; }

        /// <summary>
        /// Bool denoting button highlight state
        /// </summary>
        bool highlighted { get; set; }

        /// <summary>
        /// Bool denoting button interactable state
        /// </summary>
        bool interactable { get; set; }

        /// <summary>
        /// Bool denoting the secondary button highlight state
        /// </summary>
        bool secondaryButtonHighlighted { get; }

        /// <summary>
        /// Bool denoting that this button represents the active tool
        /// </summary>
        bool isActiveTool { set; }

        /// <summary>
        /// Bool denoting that the tooltip is visible
        /// </summary>
        bool tooltipVisible { set; }

        /// <summary>
        /// Bool denoting that this button implements a secondary button
        /// The MainMenu & SelectionTool are examples of buttons that don't implement a secondary button for closing
        /// </summary>
        bool implementsSecondaryButton { set; }

        /// <summary>
        /// The scale of the ui content in the primary content container
        /// </summary>
        Vector3 primaryUIContentContainerLocalScale { get; set; }

        /// <summary>
        /// Transform used for reference when placing tooltips
        /// </summary>
        Transform tooltipTarget { set; }

        /// <summary>
        /// String description of the tool that this button represents
        /// </summary>
        string previewToolDescription { set; }

        /// <summary>
        /// Destroys this button
        /// </summary>
        Action destroy { get; }

        /// <summary>
        /// Selects the tool based on the type assigned to this button
        /// </summary>
        Action<Type> selectTool { set; }

        /// <summary>
        /// Shows all tool buttons for a given ToolsMenu
        /// </summary>
        Action<IToolsMenuButton> showAllButtons { set; }

        /// <summary>
        /// Performed when a hover exit is detected on this button
        /// </summary>
        Action hoverExit { set; }

        /// <summary>
        /// Returns the visible button count for a given ToolsMenu
        /// used by buttons to position themselves
        /// </summary>
        Func<Type, int> visibleButtonCount { set; }

        /// <summary>
        /// Closes this button
        /// </summary>
        Func<bool> closeButton { set; }

        /// <summary>
        /// Performed when a hover action is detected by this button
        /// </summary>
        event Action hovered;
    }
}
