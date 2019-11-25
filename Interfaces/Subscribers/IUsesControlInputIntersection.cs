using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to Enable/disable a given
    /// ray-origin's ability to intersect/interact with non UI objects
    /// </summary>
    public interface IUsesControlInputIntersection : IFunctionalitySubscriber<IProvidesControlInputIntersection>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesControlInputIntersection
    /// </summary>
    public static class UsesControlInputIntersectionMethods
    {
        /// <summary>
        /// Enable/disable a given ray-origin's ability to intersect/interact with non UI objects
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">RayOrigin to enable/disable</param>
        /// <param name="enabled">Enabled/disabled state of RayOrigin</param>
        public static void SetRayOriginEnabled(this IUsesControlInputIntersection user, Transform rayOrigin, bool enabled)
        {
#if !FI_AUTOFILL
            user.provider.SetRayOriginEnabled(rayOrigin, enabled);
#endif
        }
    }
}
