using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to check if a RayOrigin is in a MiniWorld
    /// </summary>
    public interface IUsesIsInMiniWorld : IFunctionalitySubscriber<IProvidesIsInMiniWorld>
    {
    }

    public static class UsesIsInMiniWorldMethods
    {
        /// <summary>
        /// Returns whether the specified ray is contained in a MiniWorld
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the ray is contained in a MiniWorld</returns>
        public static bool IsInMiniWorld(this IUsesIsInMiniWorld user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsInMiniWorld(rayOrigin);
#endif
        }
    }
}
