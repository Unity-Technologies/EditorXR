using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the spatial hash
    /// </summary>
    public interface IProvidesGetDefaultRayColor : IFunctionalityProvider
    {
      /// <summary>
      /// Get the color of the default ray
      /// </summary>
      /// <param name="rayOrigin">The ray on which to set the color</param>
      Color GetDefaultRayColor(Transform rayOrigin);
    }
}
