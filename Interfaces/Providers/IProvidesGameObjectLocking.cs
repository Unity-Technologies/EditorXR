using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to grouping
    /// </summary>
    public interface IProvidesGameObjectLocking : IFunctionalityProvider
    {
        /// <summary>
        /// Set a GameObject's locked status
        /// </summary>
        /// <param name="go">The GameObject to set locked or unlocked</param>
        /// <param name="locked">Locked or unlocked status</param>
        void SetLocked(GameObject go, bool locked);

        /// <summary>
        /// Check whether a GameObject is locked
        /// </summary>
        /// <param name="go">GameObject locked status to test</param>
        /// <returns>Whether the object is locked</returns>
        bool IsLocked(GameObject go);
    }
}
