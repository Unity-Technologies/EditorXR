using System.Collections.Generic;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provides the ability to check if scene objects are contained within a given Bounds
    /// </summary>
    public interface IProvidesCheckBounds : IFunctionalityProvider
    {
      /// <summary>
      /// Do a bounds check against all Renderers
      /// </summary>
      /// <param name="bounds">The bounds against which to test for Renderers</param>
      /// <param name="objects">The list to which intersected Renderers will be added</param>
      /// <param name="ignoreList">(optional) A list of Renderers to ignore</param>
      /// <returns>True if any objects are contained within the bounds</returns>
      bool CheckBounds(Bounds bounds, List<GameObject> objects, List<GameObject> ignoreList = null);
    }
}
