#if UNITY_EDITOR
using UnityEngine;

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
        string menuName { get; }

        /// <summary>
        /// Description of the menu whose contents will be added to the menu
        /// </summary>
        string description { get; }

        /// <summary>
        /// Bool denoting that this menu's contents are being displayed via the SpatialUI
        /// </summary>
        bool displayingSpatially { get; }
    }
}
#endif
