using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability block all UI interaction for a given rayOrigin
    /// </summary>
    public interface IUsesBlockUIInteraction : IFunctionalitySubscriber<IProvidesBlockUIInteraction>
    {
    }

    public static class UsesBlockUIInteractionMethods
    {
        /// <summary>
        /// Prevent UI interaction for a given rayOrigin
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <param name="blocked">If true, UI interaction will be blocked for the rayOrigin.  If false, the ray origin will be removed from the blocked collection.</param>
        public static void SetUIBlockedForRayOrigin(this IUsesBlockUIInteraction user, Transform rayOrigin, bool blocked)
        {
#if !FI_AUTOFILL
            user.provider.SetUIBlockedForRayOrigin(rayOrigin, blocked);
#endif
        }
    }
}
