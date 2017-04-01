#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class WorkspaceModule : MonoBehaviour, IConnectInterfaces, ISerializePreferences
	{
		[Serializable]
		class Preferences
		{
			[SerializeField]
			List<WorkspaceLayout> m_WorkspaceLayouts = new List<WorkspaceLayout>();

			public List<WorkspaceLayout> workspaceLayouts { get { return m_WorkspaceLayouts; } }
		}

		[Serializable]
		class WorkspaceLayout
		{
			[SerializeField]
			string m_Name;
			[SerializeField]
			Vector3 m_LocalPosition;
			[SerializeField]
			Quaternion m_LocalRotation;
			[SerializeField]
			Bounds m_Bounds;
			[SerializeField]
			string m_PayloadType;
			[SerializeField]
			string m_Payload;

			public string name { get { return m_Name; } set { m_Name = value; } }
			public Vector3 localPosition { get { return m_LocalPosition; } set { m_LocalPosition = value; } }
			public Quaternion localRotation { get { return m_LocalRotation; } set { m_LocalRotation = value; } }
			public Bounds bounds { get { return m_Bounds; } set { m_Bounds = value; } }
			public string payloadType { get { return m_PayloadType; } set { m_PayloadType = value; } }
			public string payload { get { return m_Payload; } set { m_Payload = value; } }
		}

		internal static readonly Vector3 DefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
		internal static readonly Quaternion DefaultWorkspaceTilt = Quaternion.AngleAxis(-45, Vector3.right);

		internal List<IWorkspace> workspaces { get { return m_Workspaces; } }
		readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();

		internal event Action<IWorkspace> workspaceCreated;
		internal event Action<IWorkspace> workspaceDestroyed;

		internal static List<Type> workspaceTypes { get; private set; }

		public bool preserveWorkspaces { get; set; }

		static WorkspaceModule()
		{
			workspaceTypes = ObjectUtils.GetImplementationsOfInterface(typeof(IWorkspace)).ToList();
		}

		public WorkspaceModule()
		{
			preserveWorkspaces = true;
		}

		public object OnSerializePreferences()
		{
			if (!preserveWorkspaces)
				return null;

			var preferences = new Preferences();
			var workspaceLayouts = preferences.workspaceLayouts;
			foreach (var workspace in workspaces)
			{
				var layout = new WorkspaceLayout();
				layout.name = workspace.GetType().FullName;
				layout.localPosition = workspace.transform.localPosition;
				layout.localRotation = workspace.transform.localRotation;

				var ws = workspace as Workspace;
				if (ws != null)
					layout.bounds = ws.contentBounds;

				var serializeWorkspace = workspace as ISerializeWorkspace;
				if (serializeWorkspace != null)
				{
					var payload = serializeWorkspace.OnSerializeWorkspace();
					layout.payloadType = payload.GetType().FullName;
					layout.payload = JsonUtility.ToJson(payload);
				}

				workspaceLayouts.Add(layout);
			}

			return preferences;
		}

		public void OnDeserializePreferences(object obj)
		{
			if (!preserveWorkspaces)
				return;

			var preferences = (Preferences)obj;

			foreach (var workspaceLayout in preferences.workspaceLayouts)
			{
				var layout = workspaceLayout;
				CreateWorkspace(Type.GetType(workspaceLayout.name), (workspace) =>
				{
					workspace.transform.localPosition = layout.localPosition;
					workspace.transform.localRotation = layout.localRotation;

					var ws = workspace as Workspace;
					if (ws != null)
						ws.contentBounds = layout.bounds;

					var serializeWorkspace = workspace as ISerializeWorkspace;
					if (serializeWorkspace != null)
					{
						var payload = JsonUtility.FromJson(layout.payload, Type.GetType(layout.payloadType));
						serializeWorkspace.OnDeserializeWorkspace(payload);
					}
				});
			}
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
			ResetRotation(workspace, cameraTransform.forward);

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

		internal void ResetWorkspaceRotations()
		{
			var cameraTransform = CameraUtils.GetMainCamera().transform;
			foreach (var ws in workspaces)
			{
				var forward = (ws.transform.position - cameraTransform.position).normalized;
				ResetRotation(ws, forward);
			}
		}

		static void ResetRotation(IWorkspace workspace, Vector3 forward)
		{
			workspace.transform.rotation = Quaternion.LookRotation(forward) * DefaultWorkspaceTilt;
		}
	}
}
#endif
