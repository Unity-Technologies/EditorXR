using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesRaycastResults : IFunctionalitySubscriber<IProvidesRaycastResults>
    {
    }

    public static class UsesRaycastResultsMethods
    {
        /// <summary>
        /// Method used to test hover/intersection
        /// Returns the first GameObject being hovered over, or intersected with
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin for intersection purposes</param>
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
