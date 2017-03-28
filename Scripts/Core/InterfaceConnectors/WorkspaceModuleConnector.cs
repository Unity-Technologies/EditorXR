using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class WorkspaceModuleConnector : Nested, ILateBindInterfaceMethods<WorkspaceModule>
		{
			public void LateBindInterfaceMethods(WorkspaceModule provider)
			{
				ICreateWorkspaceMethods.createWorkspace = provider.CreateWorkspace;
				IMoveWorkspacesMethods.resetWorkspaces = provider.ResetWorkspaces;
				IGetAllWorkspacesMethods.getAllWorkspaces = () => provider.workspaces;
			}
		}
	}
}
