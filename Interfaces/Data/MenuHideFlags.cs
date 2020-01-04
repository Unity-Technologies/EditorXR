using System;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Flags to describe why a menu is hidden. Anything > 0 is hidden
    /// </summary>
    [Flags]
    public enum MenuHideFlags
    {
        /// <summary>
        /// The menu has been explicitly set to hidden
        /// </summary>
        Hidden = 1 << 0,

        /// <summary>
        /// The menu overlaps with another menu
        /// </summary>
        OtherMenu = 1 << 1,

        /// <summary>
        /// The ray associated with menu is hovering over UI
        /// </summary>
        OverUI = 1 << 2,

        /// <summary>
        /// The menu is overlapping with a workspace
        /// </summary>
        OverWorkspace = 1 << 3,

        /// <summary>
        /// The ray has a direct selection
        /// </summary>
        HasDirectSelection = 1 << 4,

        /// <summary>
        /// The menu is temporarily hidden
        /// </summary>
        Temporary = OtherMenu | OverUI | OverWorkspace | HasDirectSelection,

        /// <summary>
        /// The menu is occluded
        /// </summary>
        Occluded = OverUI | OverWorkspace | HasDirectSelection
    }
}
