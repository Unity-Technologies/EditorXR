using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    /// <summary>
    /// Provides UpdateInspectors method used to update inspectors when their content has changed
    /// </summary>
    public interface IUpdateInspectors
    {
    }

    public static class IUpdateInspectorsMethods
    {
        internal static Action<GameObject, bool> updateInspectors;

        /// <summary>
        /// Update all inspectors or inspectors of the specified object
        /// </summary>
        /// <param name="go">(Optional) Only update inspectors on this object. If null, update all inspectors</param>
        /// <param name="fullRebuild">(Optional) Whether to rebuild the whole inspector (for added/removed components)</param>
        public static void UpdateInspectors(this IUpdateInspectors obj, GameObject go = null, bool fullRebuild = false)
        {
            updateInspectors(go, fullRebuild);
        }
    }
}
