
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates types that need to connect interfaces for spawned objects
    /// </summary>
    public interface IInstantiateUI
    {
    }

    public static class IInstantiateUIMethods
    {
        internal delegate GameObject InstantiateUIDelegate(GameObject prefab, Transform parent = null, bool worldPositionStays = true, Transform rayOrigin = null);

        internal static InstantiateUIDelegate instantiateUI { get; set; }

        /// <summary>
        /// Method provided by the system for instantiating UI
        /// </summary>
        /// <param name="prefab">The prefab to instantiate</param>
        /// <param name="parent">(Optional) A parent transform to instantiate under</param>
        /// <param name="worldPositionStays">(Optional) If true, the parent-relative position, scale and rotation are modified
        /// <param name="rayOrigin">(Optional) RayOrigin override that will be used when connecting interfaces on this object
        /// such that the object keeps the same world space position, rotation and scale as before.</param>
        /// <returns></returns>
        public static GameObject InstantiateUI(this IInstantiateUI obj, GameObject prefab, Transform parent = null, bool worldPositionStays = true, Transform rayOrigin = null)
        {
            return instantiateUI(prefab, parent, worldPositionStays, rayOrigin);
        }
    }
}

