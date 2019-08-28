using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to the default ray color
    /// </summary>
    public interface IUsesGetDefaultRayColor : IFunctionalitySubscriber<IProvidesGetDefaultRayColor>
    {
    }

    public static class UsesGetDefaultRayColorMethods
    {
        /// /// <summary>
        /// Get the color of the default ray
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The ray whose color to get</param>
        /// <returns>The color of the default ray for the given ray origin</returns>
        public static Color GetDefaultRayColor(this IUsesGetDefaultRayColor user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(Color);
#else
            return user.provider.GetDefaultRayColor(rayOrigin);
#endif
        }
    }
}
