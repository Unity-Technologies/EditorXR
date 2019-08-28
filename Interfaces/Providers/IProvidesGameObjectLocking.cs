using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
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
        bool IsLocked(GameObject go);
    }
}
