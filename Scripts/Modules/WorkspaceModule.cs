#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class WorkspaceModule : MonoBehaviour, IConnectInterfaces
	{
		internal static readonly Vector3 k_DefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
		internal static readonly Quaternion k_DefaultWorkspaceTilt = Quaternion.AngleAxis(-20, Vector3.right);

		internal List<IWorkspace> workspaces { get { return m_Workspaces; } }
		readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();

		internal event Action<IWorkspace> workspaceCreated;
		internal event Action<IWorkspace> workspaceDestroyed;

		static internal List<Type> workspaceTypes { get; private set; }

		static WorkspaceModule()
		{
			workspaceTypes = ObjectUtils.GetImplementationsOfInterface(typeof(IWorkspace)).ToList();
		}

		internal void CreateWorkspace(Type t, Action<IWorkspace> createdCallback = null)
		{
			var cameraTransform = CameraUtils.GetMainCamera().transform;

			var workspace = (IWorkspace)ObjectUtils.CreateGameObjectWithComponent(t, CameraUtils.GetCameraRig(), false);
			m_Workspaces.Add(workspace);
			workspace.destroyed += OnWorkspaceDestroyed;
			this.ConnectInterfaces(workspace);

			//Explicit setup call (instead of setting up in Awake) because we need interfaces to be hooked up first
			workspace.Setup();

			var offset = k_DefaultWorkspaceOffset;
			offset.z += workspace.vacuumBounds.extents.z;

			var workspaceTransform = workspace.transform;
			workspaceTransform.position = cameraTransform.TransformPoint(offset);
			workspaceTransform.rotation = Quaternion.LookRotation(cameraTransform.forward) * k_DefaultWorkspaceTilt;

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
#endif
