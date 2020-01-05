using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to check if a ray is hovering over UI
    /// </summary>
    public interface IUsesIsHoveringOverUI : IFunctionalitySubscriber<IProvidesIsHoveringOverUI>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesIsHoveringOverUI
    /// </summary>
    public static class UsesIsHoveringOverUIMethods
    {
        /// <summary>
        /// Returns whether the specified ray origin is hovering over a UI element
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the ray is hovering over UI</returns>
        public static bool IsHoveringOverUI(this IUsesIsHoveringOverUI user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsHoveringOverUI(rayOrigin);
#endif
        }
    }
}
