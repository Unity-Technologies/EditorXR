
using System;

namespace UnityEditor.Experimental.EditorVR
{
    public interface IResetWorkspaces
    {
    }

    public static class IResetWorkspacesMethods
    {
        internal static Action resetWorkspaceRotations { get; set; }

        /// <summary>
        /// Reset all open workspaces
        /// </summary>
        public static void ResetWorkspaceRotations(this IResetWorkspaces obj)
        {
            resetWorkspaceRotations();
        }
    }
}

