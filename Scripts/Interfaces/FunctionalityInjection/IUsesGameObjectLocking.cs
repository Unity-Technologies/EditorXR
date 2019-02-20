using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Get access to locking features
    /// </summary>
    public interface IUsesGameObjectLocking
    {
    }

    public static class IUsesGameObjectLockingMethods
    {
        internal static Action<GameObject, bool> setLocked { get; set; }
        internal static Func<GameObject, bool> isLocked { get; set; }

        /// <summary>
        /// Set a GameObject's locked status
        /// </summary>
        /// <param name="go">The GameObject to set locked or unlocked</param>
        /// <param name="locked">Locked or unlocked status</param>
        public static void SetLocked(this IUsesGameObjectLocking obj, GameObject go, bool locked)
        {
            setLocked(go, locked);
        }

        /// <summary>
        /// Check whether a GameObject is locked
        /// </summary>
        /// <param name="go">GameObject locked status to test</param>
        public static bool IsLocked(this IUsesGameObjectLocking obj, GameObject go)
        {
            return isLocked(go);
        }
    }
}
