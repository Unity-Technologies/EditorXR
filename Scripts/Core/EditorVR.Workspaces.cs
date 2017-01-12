#if !UNITY_EDITORVR
#pragma warning disable 67, 414, 649
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Helpers;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;

namespace UnityEditor.Experimental.EditorVR
{
	internal partial class EditorVR : MonoBehaviour
	{
		static readonly Vector3 kDefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
		static readonly Quaternion kDefaultWorkspaceTilt = Quaternion.AngleAxis(-20, Vector3.right);

		List<Type> m_AllWorkspaceTypes;

		readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();
		readonly List<IVacuumable> m_Vacuumables = new List<IVacuumable>();

#if UNITY_EDITORVR
		void CreateWorkspace(Type t, Action<IWorkspace> createdCallback = null)
		{
			var cameraTransform = U.Camera.GetMainCamera().transform;

			var workspace = (IWorkspace)U.Object.CreateGameObjectWithComponent(t, U.Camera.GetViewerPivot());
			m_Workspaces.Add(workspace);
			workspace.destroyed += OnWorkspaceDestroyed;
			ConnectInterfaces(workspace);

			//Explicit setup call (instead of setting up in Awake) because we need interfaces to be hooked up first
			workspace.Setup();

			var offset = kDefaultWorkspaceOffset;
			offset.z += workspace.vacuumBounds.extents.z;

			var workspaceTransform = workspace.transform;
			workspaceTransform.position = cameraTransform.TransformPoint(offset);
			workspaceTransform.rotation *= Quaternion.LookRotation(cameraTransform.forward) * kDefaultWorkspaceTilt;

			m_Vacuumables.Add(workspace);

			if (createdCallback != null)
				createdCallback(workspace);

			// MiniWorld is a special case that we handle due to all of the mini world interactions
			var miniWorldWorkspace = workspace as MiniWorldWorkspace;
			if (!miniWorldWorkspace)
				return;

			var miniWorld = miniWorldWorkspace.miniWorld;
			m_MiniWorlds.Add(miniWorld);

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				var miniWorldRayOrigin = InstantiateMiniWorldRay();
				miniWorldRayOrigin.parent = workspace.transform;

#if ENABLE_MINIWORLD_RAY_SELECTION
				// Use the mini world ray origin instead of the original ray origin
				m_InputModule.AddRaycastSource(proxy, rayOriginPair.Key, deviceData.uiInput, miniWorldRayOrigin, (source) =>
				{
					if (!IsRayActive(source.rayOrigin))
						return false;

					if (source.hoveredObject)
						return !m_Workspaces.Any(w => source.hoveredObject.transform.IsChildOf(w.transform));

					return true;
				});
#endif

				var tester = miniWorldRayOrigin.GetComponentInChildren<IntersectionTester>();
				tester.active = false;

				m_MiniWorldRays[miniWorldRayOrigin] = new MiniWorldRay
				{
					originalRayOrigin = rayOriginPair.Value,
					miniWorld = miniWorld,
					proxy = proxy,
					node = rayOriginPair.Key,
					directSelectInput = deviceData.directSelectInput,
					tester = tester
				};

				m_IntersectionModule.AddTester(tester);
			}, false);

			UpdatePlayerHandleMaps();
		}

				private void OnWorkspaceDestroyed(IWorkspace workspace)
		{
			m_Workspaces.Remove(workspace);
			m_Vacuumables.Remove(workspace);

			DisconnectInterfaces(workspace);

			var projectFolderList = workspace as IUsesProjectFolderData;
			if (projectFolderList != null)
				m_ProjectFolderLists.Remove(projectFolderList);

			var filterUI = workspace as IFilterUI;
			if (filterUI != null)
				m_FilterUIs.Remove(filterUI);

			var miniWorldWorkspace = workspace as MiniWorldWorkspace;
			if (miniWorldWorkspace != null)
			{
				var miniWorld = miniWorldWorkspace.miniWorld;

				//Clean up MiniWorldRays
				m_MiniWorlds.Remove(miniWorld);
				var miniWorldRaysCopy = new Dictionary<Transform, MiniWorldRay>(m_MiniWorldRays);
				foreach (var ray in miniWorldRaysCopy)
				{
					var miniWorldRay = ray.Value;
					if (miniWorldRay.miniWorld == miniWorld)
					{
						var rayOrigin = ray.Key;
#if ENABLE_MINIWORLD_RAY_SELECTION
						m_InputModule.RemoveRaycastSource(rayOrigin);
#endif
						m_MiniWorldRays.Remove(rayOrigin);
					}
				}
			}
		}

#endif
	}
}
