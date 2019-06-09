using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the spatial hash
    /// </summary>
    public interface IProvidesSpatialHash : IFunctionalityProvider
    {
        /// <summary>
        /// Add all renderers of a GameObject (and its children) to the spatial hash for queries, direct selection, etc.
        /// </summary>
        /// <param name="go">The GameObject to add</param>
        void AddToSpatialHash(GameObject go);

        /// <summary>
        /// Remove all renderers of a GameObject (and its children) from the spatial hash
        /// </summary>
        /// <param name="go">The GameObject to remove</param>
        void RemoveFromSpatialHash(GameObject go);
    }
}
