using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to check if the main menu is visible
    /// </summary>
    public interface IUsesIsMainMenuVisible : IFunctionalitySubscriber<IProvidesIsMainMenuVisible>
    {
    }

    public static class UsesIsMainMenuVisibleMethods
    {
        /// <summary>
        /// Returns whether the main menu is visible on the specified rayOrigin
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        public static bool IsMainMenuVisible(this IUsesIsMainMenuVisible user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsMainMenuVisible(rayOrigin);
#endif
        }
    }
}
