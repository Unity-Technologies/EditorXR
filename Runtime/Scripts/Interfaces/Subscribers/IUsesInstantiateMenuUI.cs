using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to viewer scale
    /// </summary>
    public interface IUsesInstantiateMenuUI : IFunctionalitySubscriber<IProvidesInstantiateMenuUI>
    {
    }

    /// <summary>
    /// Extension methods for users of IUsesInstantiateMenuUI
    /// </summary>
    public static class UsesInstantiateMenuUIMethods
    {
        /// <summary>
        /// Instantiate custom menu UI on a proxy
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The ray origin of the proxy that this menu is being instantiated from</param>
        /// <param name="menuPrefab">The prefab (with an IMenu component) to instantiate</param>
        /// <returns>The instantiated object</returns>
        public static GameObject InstantiateMenuUI(this IUsesInstantiateMenuUI user, Transform rayOrigin, IMenu menuPrefab)
        {
#if FI_AUTOFILL
            return default(GameObject);
#else
            return user.provider.InstantiateMenuUI(rayOrigin, menuPrefab);
#endif
        }
    }
}
