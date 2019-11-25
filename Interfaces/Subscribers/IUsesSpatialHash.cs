using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesSpatialHash : IFunctionalitySubscriber<IProvidesSpatialHash>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesSpatialHash
    /// </summary>
    public static class UsesSpatialHashMethods
    {
        /// <summary>
        /// Add all renderers of a GameObject (and its children) to the spatial hash for queries, direct selection, etc.
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="go">The GameObject to add</param>
        public static void AddToSpatialHash(this IUsesSpatialHash user, GameObject go)
        {
#if !FI_AUTOFILL
            user.provider.AddToSpatialHash(go);
#endif
        }

        /// <summary>
        /// Remove all renderers of a GameObject (and its children) from the spatial hash
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="go">The GameObject to remove</param>
        public static void RemoveFromSpatialHash(this IUsesSpatialHash user, GameObject go)
        {
#if !FI_AUTOFILL
            user.provider.RemoveFromSpatialHash(go);
#endif
        }
    }
}
