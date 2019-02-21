using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Make use of the spatial hash
    /// </summary>
    public interface IUsesSpatialHash
    {
    }

    public static class IUsesSpatialHashMethods
    {
        internal static Action<GameObject> addToSpatialHash { get; set; }
        internal static Action<GameObject> removeFromSpatialHash { get; set; }

        /// <summary>
        /// Add all renderers of a GameObject (and its children) to the spatial hash for queries, direct selection, etc.
        /// </summary>
        /// <param name="go">The GameObject to add</param>
        public static void AddToSpatialHash(this IUsesSpatialHash obj, GameObject go)
        {
            addToSpatialHash(go);
        }

        /// <summary>
        /// Remove all renderers of a GameObject (and its children) from the spatial hash
        /// </summary>
        /// <param name="go">The GameObject to remove</param>
        public static void RemoveFromSpatialHash(this IUsesSpatialHash obj, GameObject go)
        {
            removeFromSpatialHash(go);
        }
    }
}
