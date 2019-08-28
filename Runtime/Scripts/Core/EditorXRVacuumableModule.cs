
using Unity.Labs.EditorXR.Interfaces;
#if UNITY_2018_3_OR_NEWER
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    class EditorXRVacuumableModule : IModuleDependency<WorkspaceModule>
    {
        readonly List<IVacuumable> m_Vacuumables = new List<IVacuumable>();

        public List<IVacuumable> vacuumables { get { return m_Vacuumables; } }

        void OnWorkspaceCreated(IWorkspace workspace)
        {
            m_Vacuumables.Add(workspace);
        }

        void OnWorkspaceDestroyed(IWorkspace workspace)
        {
            m_Vacuumables.Remove(workspace);
        }

        public void ConnectDependency(WorkspaceModule dependency)
        {
            dependency.workspaceCreated += OnWorkspaceCreated;
            dependency.workspaceDestroyed += OnWorkspaceDestroyed;
        }

        public void LoadModule() { }

        public void UnloadModule() { }
    }
}
#endif
