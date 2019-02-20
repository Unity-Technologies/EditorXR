using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates types that can provide an item on the Settings menu
    /// </summary>
    public interface ISettingsMenuItemProvider
    {
        /// <summary>
        /// The menu face prefab which will be added to the menu
        /// </summary>
        GameObject settingsMenuItemPrefab { get; }

        /// <summary>
        /// An instance of the menu face prefab that was added to the menu
        /// May be null if menu item could not be added to menu
        /// </summary>
        GameObject settingsMenuItemInstance { set; }

        /// <summary>
        /// The rayOrigin this provider is associated with
        /// This will determine which menu is used. If null, both menus are used
        /// </summary>
        Transform rayOrigin { get; }
    }
}
