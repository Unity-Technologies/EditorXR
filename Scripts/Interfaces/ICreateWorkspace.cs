#if UNITY_EDITOR
using System;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Method signature for creating workspaces
	/// </summary>
	/// <param name="type">Type of the workspace (must inherit from Workspace)</param>
	/// <param name="createdCallback">Called once the workspace is created</param>
	public delegate void CreateWorkspaceDelegate(Type type, Action<IWorkspace> createdCallback = null);

	/// <summary>
	/// Create workspaces
	/// </summary>
	public interface ICreateWorkspace
	{
		/// <summary>
		/// Method provided by the system for creating workspaces
		/// </summary>
		CreateWorkspaceDelegate createWorkspace { set; }
	}
}
#endif
