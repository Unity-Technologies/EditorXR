#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides custom menu instantiation
    /// </summary>
    public interface IInstantiateMenuUI
    {
    }

    public static class IInstantiateMenuUIMethods
    {
        internal static Func<Transform, IMenu, GameObject> instantiateMenuUI { get; set; }

        /// <summary>
        /// Instantiate custom menu UI on a proxy
        /// </summary>
        /// <param name="rayOrigin">The ray origin of the proxy that this menu is being instantiated from</param>
        /// <param name="menuPrefab">The prefab (with an IMenu component) to instantiate</param>
        public static GameObject InstantiateMenuUI(this IInstantiateMenuUI obj, Transform rayOrigin, IMenu menuPrefab)
        {
            return instantiateMenuUI(rayOrigin, menuPrefab);
        }
    }
}
#endif
