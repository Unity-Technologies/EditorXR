#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to the Web Module
    /// </summary>
    public interface IUsesGrouping
    {
    }

    public static class IUsesGroupingMethods
    {
        internal static Action<GameObject> makeGroup;

        public static void MakeGroup(this IUsesGrouping obj, GameObject parent)
        {
            makeGroup(parent);
        }
    }
}
#endif
