using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Experimental.EditorVR.Helpers;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	internal class WorkspaceModule : MonoBehaviour, IConnectInterfaces
	{
		internal static readonly Vector3 kDefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
		static readonly Quaternion kDefaultWorkspaceTilt = Quaternion.AngleAxis(-20, Vector3.right);

		internal List<IWorkspace> workspaces { get { return m_Workspaces; } }
		readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();

		internal event Action<IWorkspace> workspaceCreated;
		internal event Action<IWorkspace> workspaceDestroyed;

		internal List<Type> workspaceTypes { get; private set; }

		public ConnectInterfacesDelegate connectInterfaces { private get; set; }

		public WorkspaceModule()
		{
			workspaceTypes = U.Object.GetImplementationsOfInterface(typeof(IWorkspace)).ToList();
		}

		internal void CreateWorkspace(Type t, Action<IWorkspace> createdCallback = null)
		{
			var cameraTransform = U.Camera.GetMainCamera().transform;

			var workspace = (IWorkspace)U.Object.CreateGameObjectWithComponent(t, U.Camera.GetViewerPivot());
			m_Workspaces.Add(workspace);
			workspace.destroyed += OnWorkspaceDestroyed;
			connectInterfaces(workspace);

			//Explicit setup call (instead of setting up in Awake) because we need interfaces to be hooked up first
			workspace.Setup();

			var offset = kDefaultWorkspaceOffset;
			offset.z += workspace.vacuumBounds.extents.z;

			var workspaceTransform = workspace.transform;
			workspaceTransform.position = cameraTransform.TransformPoint(offset);
			workspaceTransform.rotation *= Quaternion.LookRotation(cameraTransform.forward) * kDefaultWorkspaceTilt;

			if (createdCallback != null)
				createdCallback(workspace);

			if (workspaceCreated != null)
				workspaceCreated(workspace);
		}

		void OnWorkspaceDestroyed(IWorkspace workspace)
		{
			m_Workspaces.Remove(workspace);

			if (workspaceDestroyed != null)
				workspaceDestroyed(workspace);
		}
	}
}