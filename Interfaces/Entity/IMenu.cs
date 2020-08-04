using UnityEngine;

namespace Unity.EditorXR.Interfaces
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

        /// <summary>
        /// The priority of this menu for deciding which menu should be visible if multiple menus overlap
        /// </summary>
        int priority { get; }
    }
}
