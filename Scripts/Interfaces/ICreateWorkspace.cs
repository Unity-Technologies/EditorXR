using System;
using UnityEngine.VR.Workspaces;

namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Method signature for creating workspaces
	/// </summary>
	/// <param name="type">Type of the workspace (must inherit from Workspace)</param>
	/// <param name="createdCallback">Called once the workspace is created</param>
	public delegate void CreateWorkspaceDelegate(Type type, Action<Workspace> createdCallback = null);

	/// <summary>
	/// Decorates types that need to create workspaces
	/// </summary>
	public interface ICreateWorkspace
	{
		/// <summary>
		/// Method provided by the system for creating workspaces
		/// </summary>
		CreateWorkspaceDelegate createWorkspace { set; }
	}
}