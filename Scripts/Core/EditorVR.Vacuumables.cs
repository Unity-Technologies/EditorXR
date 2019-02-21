using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class Vacuumables : Nested
        {
            public List<IVacuumable> vacuumables { get { return m_Vacuumables; } }

            readonly List<IVacuumable> m_Vacuumables = new List<IVacuumable>();

            internal void OnWorkspaceCreated(IWorkspace workspace)
            {
                m_Vacuumables.Add(workspace);
            }

            internal void OnWorkspaceDestroyed(IWorkspace workspace)
            {
                m_Vacuumables.Remove(workspace);
            }
        }
    }
}
