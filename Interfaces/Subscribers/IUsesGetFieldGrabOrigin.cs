using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to get the preview origins
    /// </summary>
    public interface IUsesGetFieldGrabOrigin : IFunctionalitySubscriber<IProvidesGetFieldGrabOrigin>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesGetFieldGrabOrigin
    /// </summary>
    public static class UsesGetFieldDragOriginMethods
    {
        /// <summary>
        /// Get the field grab transform attached to the given rayOrigin
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin that is grabbing the field</param>
        /// <returns>The field grab origin</returns>
        public static Transform GetFieldGrabOriginForRayOrigin(this IUsesGetFieldGrabOrigin user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(Transform);
#else
            return user.provider.GetFieldGrabOriginForRayOrigin(rayOrigin);
#endif
        }
    }
}
