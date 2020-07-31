using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to grouping
    /// </summary>
    public interface IProvidesGrouping : IFunctionalityProvider
    {
      /// <summary>
      /// Make this object, and its children into a group
      /// </summary>
      /// <param name="root">The root of the group</param>
      void MakeGroup(GameObject root);
    }
}
