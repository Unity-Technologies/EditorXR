#if UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Helpers;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Proxies;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
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
#endif
