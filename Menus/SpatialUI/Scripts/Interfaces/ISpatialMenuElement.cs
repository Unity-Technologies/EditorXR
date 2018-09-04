#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    public interface ISpatialMenuElement
    {
        /// <summary>
        /// Gameobject housing visual/UI content
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Bool denoting that this element is currently highlighted
        /// </summary>
        bool highlighted { set; }

        /// <summary>
        /// Bool denoting that this element is currently visible
        /// </summary>
        bool visible { set; }

        /// <summary>
        /// FUnction that sets up the model and view for this particular element
        /// </summary>
        Action<Transform, Action, string, string> Setup { get; }

        /// <summary>
        /// Action performed when this element is selected
        /// The node denotes either the controlling SpatialMenu's node,
        /// or the node of a hovering proxy (which takes precedence over the menu control node)
        /// The main purpose of the node is to allow a selected action to perform a
        /// rayOriginal dependent actions (selecting & assigning a tools to a given proxy, etc)
        /// </summary>
        Action<Node> selected { get; set; }

        /// <summary>
        /// Action performed when this element is highlighted
        /// </summary>
        Action<SpatialMenu.SpatialMenuData> highlightedAction { get; set; }

        /// <summary>
        /// Reference to the data defining the parent menu of this element
        /// Used to display certain relevant visual elements relating to the parent menu
        /// </summary>
        SpatialMenu.SpatialMenuData parentMenuData { get; set; }

        /// <summary>
        /// If the menu element isn't being hovered, utilize this node for performing any node-dependent logic
        /// </summary>
        Node spatialMenuActiveControllerNode { get; set; }
    }
}
#endif
