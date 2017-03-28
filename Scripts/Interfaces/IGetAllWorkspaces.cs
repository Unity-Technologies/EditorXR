#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Get all open workspaces
	/// </summary>
	public interface IGetAllWorkspaces
	{
	}

	static class IGetAllWorkspacesMethods
	{
		internal static Func<List<IWorkspace>> getAllWorkspaces { get; set; }

		/// <summary>
		/// Returns all open workspaces
		/// </summary>
		public static List<IWorkspace> GetAllWorkspaces(this IGetAllWorkspaces obj)
		{
			return getAllWorkspaces();
		}
	}
}
#endif
