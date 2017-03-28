using System;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IMoveWorkspaces
	{
	}

	public static class IMoveWorkspacesMethods
	{
		internal static Action resetWorkspaces { get; set; }

		/// <summary>
		/// Reset all open workspaces
		/// </summary>
		public static void ResetWorkspaces(this IMoveWorkspaces obj)
		{
			resetWorkspaces();
		}
	}
}
