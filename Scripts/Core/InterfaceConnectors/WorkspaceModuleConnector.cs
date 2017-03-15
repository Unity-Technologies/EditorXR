using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class WorkspaceModuleConnector : Nested, IInterfaceConnector
		{
			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var evrWorkspaceModule = evr.m_WorkspaceModule;

				var createWorkspace = obj as ICreateWorkspace;
				if (createWorkspace != null)
					createWorkspace.createWorkspace = evrWorkspaceModule.CreateWorkspace;
			}

			public void DisconnectInterface(object obj)
			{
			}
		}
	}

}
