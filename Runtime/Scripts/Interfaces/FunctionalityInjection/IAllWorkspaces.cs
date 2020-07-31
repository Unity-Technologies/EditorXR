using System.Collections.Generic;
using Unity.EditorXR.Interfaces;

namespace Unity.EditorXR
{
    /// <summary>
    /// Get all open workspaces
    /// </summary>
    public interface IAllWorkspaces
    {
        /// <summary>
        /// A list containing all open workspaces
        /// </summary>
        List<IWorkspace> allWorkspaces { set; }
    }
}
