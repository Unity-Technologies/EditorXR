using System.Collections.Generic;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to check if scene objects are contained within a given Bounds
    /// </summary>
    public interface IUsesCheckBounds : IFunctionalitySubscriber<IProvidesCheckBounds>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesCheckBounds
    /// </summary>
    public static class UsesCheckBoundsMethods
    {
      /// <summary>
      /// Do a bounds check against all Renderers
      /// </summary>
      /// <param name="user">The functionality user</param>
      /// <param name="bounds">The bounds against which to test for Renderers</param>
      /// <param name="objects">The list to which intersected Renderers will be added</param>
      /// <param name="ignoreList">(optional) A list of Renderers to ignore</param>
      /// <returns>True if any objects are contained within the bounds</returns>
      public static bool CheckBounds(this IUsesCheckBounds user, Bounds bounds, List<GameObject> objects, List<GameObject> ignoreList = null)
      {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.CheckBounds(bounds, objects, ignoreList);
#endif
        }
    }
}
