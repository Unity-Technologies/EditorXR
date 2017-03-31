#if UNITY_EDITOR
using System;
using System.Collections.Generic;

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
#endif
