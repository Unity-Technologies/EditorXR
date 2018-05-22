
using System;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Create workspaces
    /// </summary>
    public interface ICreateWorkspace
    {
    }

    static class ICreateWorkspaceMethods
    {
        internal delegate void CreateWorkspaceDelegate(Type type, Action<IWorkspace> createdCallback = null);

        internal static CreateWorkspaceDelegate createWorkspace { get; set; }

        /// <summary>
        /// Method for creating workspaces
        /// </summary>
        /// <param name="type">Type of the workspace (must inherit from Workspace)</param>
        /// <param name="createdCallback">Called once the workspace is created</param>
        public static void CreateWorkspace(this ICreateWorkspace ci, Type type, Action<IWorkspace> createdCallback = null)
        {
            createWorkspace(type, createdCallback);
        }
    }
}

