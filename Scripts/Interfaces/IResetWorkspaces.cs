using System;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IResetWorkspaces
	{
	}

	public static class IResetWorkspacesMethods
	{
		internal static Action resetWorkspaces { get; set; }

		/// <summary>
		/// Reset all open workspaces
		/// </summary>
		public static void ResetWorkspaces(this IResetWorkspaces obj)
		{
			resetWorkspaces();
		}
	}
}
