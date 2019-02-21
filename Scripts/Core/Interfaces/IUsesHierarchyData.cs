using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Exposes a property used to provide a hierarchy of scene objects to the object
    /// </summary>
    interface IUsesHierarchyData
    {
        /// <summary>
        /// Set accessor for hierarchy list data
        /// Used to update existing implementors after lazy load completes
        /// </summary>
        List<HierarchyData> hierarchyData { set; }
    }
}
