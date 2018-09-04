#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    public interface ISpatialMenuElement
    {
        GameObject gameObject { get; }

        bool highlighted { set; }

        bool visible { set; }

        Button button { get; }

        Action<Transform, Action, string, string> Setup { get; }

        Action<Node> selected { get; set; }

        Action<SpatialMenu.SpatialMenuData> highlightedAction { get; set; }

        SpatialMenu.SpatialMenuData parentMenuData { get; set; }

        Action correspondingFunction { get; set; }

        Action onHiddenAction { get; set; }

        /// <summary>
        /// If the menu element isn't being hovered, utilize this node for performing any node-dependent logic
        /// </summary>
        Node spatialMenuActiveControllerNode { get; set; }

        /// <summary>
        /// If the menu element isn't being hovered, utilize this node for performing any node-dependent logic
        /// </summary>
        Node hoveringNode { get; set; }
    }
}
#endif
