using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the spatial hash
    /// </summary>
    public interface IProvidesInstantiateMenuUI : IFunctionalityProvider
    {
        /// <summary>
        /// Instantiate custom menu UI on a proxy
        /// </summary>
        /// <param name="rayOrigin">The ray origin of the proxy that this menu is being instantiated from</param>
        /// <param name="menuPrefab">The prefab (with an IMenu component) to instantiate</param>
        /// <returns>The instantiated object</returns>a
        GameObject InstantiateMenuUI(Transform rayOrigin, IMenu menuPrefab);
    }
}
