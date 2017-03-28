#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class WorkspaceModule : MonoBehaviour, IConnectInterfaces
	{
		internal static readonly Vector3 DefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
		internal static readonly Quaternion DefaultWorkspaceTilt = Quaternion.AngleAxis(-45, Vector3.right);

		internal List<IWorkspace> workspaces { get { return m_Workspaces; } }
		readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();

		internal event Action<IWorkspace> workspaceCreated;
		internal event Action<IWorkspace> workspaceDestroyed;

		internal static List<Type> workspaceTypes { get; private set; }

		static WorkspaceModule()
		{
			workspaceTypes = ObjectUtils.GetImplementationsOfInterface(typeof(IWorkspace)).ToList();
		}

		void Start()
		{
			CreateSavedWorkspaces();
		}

		void OnDestroy()
		{
			SaveWorkspacePositions();
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

			var offset = DefaultWorkspaceOffset;
			offset.z += workspace.vacuumBounds.extents.z;

			var workspaceTransform = workspace.transform;
			workspaceTransform.position = cameraTransform.TransformPoint(offset);
			ResetWorkspaceRotation(workspace, cameraTransform.forward);

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

		void CreateSavedWorkspaces()
		{
			string inputString = EditorPrefs.GetString("WorkspaceSavePositions");

			if (string.IsNullOrEmpty(inputString))
				return;

			WorkspaceSave savedData = JsonUtility.FromJson<WorkspaceSave>(inputString);
			if (savedData.workspaces != null && savedData.workspaces.Length > 0)
			{
				foreach (var wsData in savedData.workspaces)
				{
					CreateWorkspace(Type.GetType(wsData.workspaceName), (workSpace) =>
					{
						workSpace.transform.localPosition = wsData.localPosition;
						workSpace.transform.localRotation = wsData.localRotation;
						var ws = workSpace as Workspace;
						if (ws != null)
							ws.contentBounds = wsData.bounds;
					});

				}
			}
		}

		void SaveWorkspacePositions()
		{
			var workspaceSaves = new WorkspaceSave(workspaces.Count);
			var saveDatas = new List<WorkspaceSaveData>();

			foreach (var workspace in workspaces)
			{
				var saveData = new WorkspaceSaveData();
				saveData.workspaceName = workspace.GetType().ToString();
				saveData.localPosition = workspace.transform.localPosition;
				saveData.localRotation = workspace.transform.localRotation;

				var ws = workspace as Workspace;
				if (ws != null)
					saveData.bounds = ws.contentBounds;

				saveDatas.Add(saveData);
			}
			workspaceSaves.workspaces = saveDatas.ToArray();

			EditorPrefs.SetString("WorkspaceSavePositions", JsonUtility.ToJson(workspaceSaves));
		}

		internal void ResetWorkspaces()
		{
			var cameraTransform = CameraUtils.GetMainCamera().transform;
			foreach (var ws in workspaces)
			{
				var forward = (ws.transform.position - cameraTransform.position).normalized;
				ResetWorkspaceRotation(ws, forward);
			}
		}

		static void ResetWorkspaceRotation(IWorkspace workspace, Vector3 forward)
		{
			workspace.transform.rotation = Quaternion.LookRotation(forward) * DefaultWorkspaceTilt;
		}
	}
}
#endif
