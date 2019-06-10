using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesSetDefaultRayColor : IFunctionalitySubscriber<IProvidesSetDefaultRayColor>
    {
    }

    public static class UsesSetDefaultRayColorMethods
    {
      /// <summary>
      /// Set the color of the default ray
      /// </summary>
      /// <param name="user">The functionality user</param>
      /// <param name="rayOrigin">The ray on which to set the color</param>
      /// <param name="color">The color to set on the default ray</param>
      public static void SetDefaultRayColor(this IUsesSetDefaultRayColor user, Transform rayOrigin, Color color)
      {
#if !FI_AUTOFILL
            user.provider.SetDefaultRayColor(rayOrigin, color);
#endif
      }
    }
}
