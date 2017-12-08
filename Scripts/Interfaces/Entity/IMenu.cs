#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Declares a class as a system-level menu
    /// </summary>
    public interface IMenu
    {
        /// <summary>
        /// Visibility state of this menu
        /// </summary>
        MenuHideFlags menuHideFlags { get; set; }

        /// <summary>
        /// GameObject that this component is attached to
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Root GameObject for visible menu content
        /// </summary>
        GameObject menuContent { get; }

        /// <summary>
        /// The local bounds of this menu
        /// </summary>
        Bounds localBounds { get; }

        int priority { get; }
    }
}
#endif
