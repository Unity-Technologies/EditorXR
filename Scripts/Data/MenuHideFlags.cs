using System;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    /// <summary>
    /// Flags to describe why a menu is hidden. Anything > 0 is hidden
    /// </summary>
    [Flags]
    public enum MenuHideFlags
    {
        Hidden = 1 << 0,
        OtherMenu = 1 << 1,
        OverUI = 1 << 2,
        OverWorkspace = 1 << 3,
        HasDirectSelection = 1 << 4,

        Temporary = OtherMenu | OverUI | OverWorkspace | HasDirectSelection,
        Occluded = OverUI | OverWorkspace | HasDirectSelection
    }
}
