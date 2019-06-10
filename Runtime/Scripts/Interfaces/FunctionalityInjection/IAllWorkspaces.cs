using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Get all open workspaces
    /// </summary>
    public interface IAllWorkspaces
    {
        List<IWorkspace> allWorkspaces { set; }
    }
}
