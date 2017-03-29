#if UNITY_EDITOR && UNITY_EDITORVR
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class WorkspaceModuleConnector : Nested, ILateBindInterfaceMethods<WorkspaceModule>
		{
			public void LateBindInterfaceMethods(WorkspaceModule provider)
			{
				ICreateWorkspaceMethods.createWorkspace = provider.CreateWorkspace;
			}
		}
	}
}
#endif