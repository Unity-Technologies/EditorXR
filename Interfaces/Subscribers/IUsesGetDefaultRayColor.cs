using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesGetDefaultRayColor : IFunctionalitySubscriber<IProvidesGetDefaultRayColor>
    {
    }

    public static class UsesGetDefaultRayColorMethods
    {
      /// <summary>
      /// Set the color of the default ray
      /// </summary>
      /// <param name="user">The functionality user</param>
      /// <param name="rayOrigin">The ray on which to set the color</param>
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
