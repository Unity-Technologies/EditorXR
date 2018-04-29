#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates types that can provide a sub-menu in the SpatialUI
    /// </summary>
    public interface ISpatialMenuProvider
    {
        /// <summary>
        /// Name of the menu whose contents will be added to the menu
        /// </summary>
        string spatialMenuName { get; }

        /// <summary>
        /// Description of the menu whose contents will be added to the menu
        /// </summary>
        string spatialMenuDescription { get; }

        /// <summary>
        /// Bool denoting that this menu's contents are being displayed via the SpatialUI
        /// </summary>
        bool displayingSpatially { get; set; }

        /// <summary>
        /// Collection of elements with which to populate the corresponding spatial UI table/list/view
        /// </summary>
        List<SpatialMenu.SpatialUITableElement> spatialTableElements { get; }
    }
}
#endif
