using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to grouping
    /// </summary>
    public interface IUsesGameObjectLocking : IFunctionalitySubscriber<IProvidesGameObjectLocking>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesGameObjectLocking
    /// </summary>
    public static class UsesGameObjectLockingMethods
    {
        /// <summary>
        /// Set a GameObject's locked status
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="go">The GameObject to set locked or unlocked</param>
        /// <param name="locked">Locked or unlocked status</param>
        public static void SetLocked(this IUsesGameObjectLocking user, GameObject go, bool locked)
        {
            {
#if !FI_AUTOFILL
                user.provider.SetLocked(go, locked);
#endif
            }
        }

        /// <summary>
        /// Check whether a GameObject is locked
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="go">GameObject locked status to test</param>
        /// <returns>Whether the GameObject is locked</returns>
        public static bool IsLocked(this IUsesGameObjectLocking user, GameObject go)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsLocked(go);
#endif
        }
    }
}
