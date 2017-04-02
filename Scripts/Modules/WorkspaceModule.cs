#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class WorkspaceModule : MonoBehaviour, IConnectInterfaces
	{
		internal static readonly Vector3 k_DefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
		internal static readonly Quaternion k_DefaultWorkspaceTilt = Quaternion.AngleAxis(-20, Vector3.right);

		internal List<IWorkspace> workspaces { get { return m_Workspaces; } }
		readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();

		internal List<WorkspaceInput> workspaceInputs { get { return m_WorkspaceInputs; } }
		readonly List<WorkspaceInput> m_WorkspaceInputs = new List<WorkspaceInput>();

		internal event Action<IWorkspace> workspaceCreated;
		internal event Action<IWorkspace> workspaceDestroyed;

		internal static List<Type> workspaceTypes { get; private set; }

		internal Transform leftRayOrigin { private get; set; }
		internal Transform rightRayOrigin { private get; set; }

		public Func<Transform, float> getPointerLength { private get; set; }

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
			workspace.leftRayOrigin = leftRayOrigin;
			workspace.rightRayOrigin = rightRayOrigin;
			workspace.getPointerLength = getPointerLength;

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

		internal void ProcessInputInWorkspaces(ConsumeControlDelegate consumeControl)
		{
			for (int i = 0; i < m_Workspaces.Count; i++)
			{
				m_Workspaces[i].ProcessInput(m_WorkspaceInputs[i], consumeControl);
			}
		}

		void OnWorkspaceDestroyed(IWorkspace workspace)
		{
			m_Workspaces.Remove(workspace);

			this.DisonnectInterfaces(workspace);

			if (workspaceDestroyed != null)
				workspaceDestroyed(workspace);
		}
	}
}
#endif
