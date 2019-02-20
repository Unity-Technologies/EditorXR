using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to grouping
    /// </summary>
    public interface IUsesGrouping
    {
    }

    public static class IUsesGroupingMethods
    {
        internal static Action<GameObject> makeGroup;

        /// <summary>
        /// Make this object, and its children into a group
        /// </summary>
        /// <param name="root">The root of the group</param>
        public static void MakeGroup(this IUsesGrouping obj, GameObject root)
        {
            makeGroup(root);
        }
    }
}
