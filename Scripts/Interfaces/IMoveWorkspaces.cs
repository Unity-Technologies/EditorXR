using System;
using UnityEngine.VR.Workspaces;

namespace UnityEngine.VR.Tools
{
	public interface IMoveWorkspaces
	{
		Action<Workspace> resetWorkspaces
		{
			get; set;
		}
	}
}
