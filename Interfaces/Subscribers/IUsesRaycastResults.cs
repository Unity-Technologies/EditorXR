using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesRaycastResults : IFunctionalitySubscriber<IProvidesRaycastResults>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesRaycastResults
    /// </summary>
    public static class UsesRaycastResultsMethods
    {
        /// <summary>
        /// Test hover/intersection
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin for intersection purposes</param>
        /// <returns>The first GameObject being hovered over, or intersected with</returns>
        public static GameObject GetFirstGameObject(this IUsesRaycastResults user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(GameObject);
#else
            return user.provider.GetFirstGameObject(rayOrigin);
#endif
        }
    }
}
