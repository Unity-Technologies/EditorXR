using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesViewerBody : IFunctionalitySubscriber<IProvidesViewerBody>
    {
    }

    public static class UsesViewerBodyMethods
    {
        /// <summary>
        /// Returns whether the specified transform is over the viewer's shoulders and behind the head
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin to test</param>
        public static bool IsOverShoulder(this IUsesViewerBody user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsOverShoulder(rayOrigin);
#endif
        }

        /// <summary>
        /// Returns whether the specified transform is over the viewer's head
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin to test</param>
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
