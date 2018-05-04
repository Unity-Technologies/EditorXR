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
        /// Collection of section elements with which to populate the spatial UI table/list/view
        /// </summary>
        List<SpatialMenu.SpatialMenuData> spatialMenuData { get; }
    }
}
#endif
