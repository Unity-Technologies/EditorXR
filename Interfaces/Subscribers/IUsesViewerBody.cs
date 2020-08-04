using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesViewerBody : IFunctionalitySubscriber<IProvidesViewerBody>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesViewerBody
    /// </summary>
    public static class UsesViewerBodyMethods
    {
        /// <summary>
        /// Check whether the specified transform is over the viewer's shoulders and behind the head
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin to test</param>
        /// <returns>Whether the specified transform is over the viewer's shoulders and behind the head</returns>
        public static bool IsOverShoulder(this IUsesViewerBody user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsOverShoulder(rayOrigin);
#endif
        }

        /// <summary>
        /// Check whether the specified transform is over the viewer's head
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin to test</param>
        /// <returns>Whether the specified transform is over the viewer's head</returns>
        public static bool IsAboveHead(this IUsesViewerBody user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsAboveHead(rayOrigin);
#endif
        }
    }
}
