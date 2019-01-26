using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Receive the default list of ignored objects from the system for intersection purposes
    /// </summary>
    public interface IStandardIgnoreList
    {
        /// <summary>
        /// List of system objects to ignore for intersection purposes
        /// </summary>
        List<GameObject> ignoreList { set; }
    }
}
