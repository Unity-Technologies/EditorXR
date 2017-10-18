#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class WorkspaceModuleConnector : Nested, ILateBindInterfaceMethods<WorkspaceModule>, IInterfaceConnector
        {
            public void LateBindInterfaceMethods(WorkspaceModule provider)
            {
                ICreateWorkspaceMethods.createWorkspace = provider.CreateWorkspace;
                IResetWorkspacesMethods.resetWorkspaceRotations = provider.ResetWorkspaceRotations;
            }

            public void ConnectInterface(object target, object userData = null)
            {
                var workspaceModule = evr.GetModule<WorkspaceModule>();

                var allWorkspaces = target as IAllWorkspaces;
                if (allWorkspaces != null)
                    allWorkspaces.allWorkspaces = workspaceModule.workspaces;
            }

            public void DisconnectInterface(object target, object userData = null) { }
        }
    }
}
#endif
