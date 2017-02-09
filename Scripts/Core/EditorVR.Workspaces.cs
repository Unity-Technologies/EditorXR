#if UNITY_EDITORVR
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
	partial class EditorVR
	{
		static readonly Vector3 kDefaultWorkspaceOffset = new Vector3(0, -0.15f, 0.4f);
		static readonly Quaternion kDefaultWorkspaceTilt = Quaternion.AngleAxis(-20, Vector3.right);

		List<Type> m_AllWorkspaceTypes;

		readonly List<IWorkspace> m_Workspaces = new List<IWorkspace>();
		readonly List<IVacuumable> m_Vacuumables = new List<IVacuumable>();

		void CreateWorkspace(Type t, Action<IWorkspace> createdCallback = null)
		{
			var cameraTransform = U.Camera.GetMainCamera().transform;

			var workspace = (IWorkspace)U.Object.CreateGameObjectWithComponent(t, U.Camera.GetViewerPivot());
			m_Workspaces.Add(workspace);
			workspace.destroyed += OnWorkspaceDestroyed;
			m_Interfaces.ConnectInterfaces(workspace);

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
			m_MiniWorlds.worlds.Add(miniWorld);

			ForEachProxyDevice((deviceData) =>
			{
				var miniWorldRayOrigin = m_MiniWorlds.InstantiateMiniWorldRay();
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

				m_MiniWorlds.rays[miniWorldRayOrigin] = new MiniWorlds.MiniWorldRay
				{
					originalRayOrigin = deviceData.rayOrigin,
					miniWorld = miniWorld,
					proxy = deviceData.proxy,
					node = deviceData.node,
					directSelectInput = deviceData.directSelectInput,
					tester = tester
				};

				m_IntersectionModule.AddTester(tester);
			}, false);

			m_DeviceInputModule.UpdatePlayerHandleMaps();
		}

		void OnWorkspaceDestroyed(IWorkspace workspace)
		{
			m_Workspaces.Remove(workspace);
			m_Vacuumables.Remove(workspace);

			m_Interfaces.DisconnectInterfaces(workspace);

			var miniWorldWorkspace = workspace as MiniWorldWorkspace;
			if (miniWorldWorkspace != null)
			{
				var miniWorld = miniWorldWorkspace.miniWorld;

				//Clean up MiniWorldRays
				m_MiniWorlds.worlds.Remove(miniWorld);
				var miniWorldRaysCopy = new Dictionary<Transform, MiniWorlds.MiniWorldRay>(m_MiniWorlds.rays);
				foreach (var ray in miniWorldRaysCopy)
				{
					var miniWorldRay = ray.Value;
					if (miniWorldRay.miniWorld == miniWorld)
					{
						var rayOrigin = ray.Key;
#if ENABLE_MINIWORLD_RAY_SELECTION
						m_InputModule.RemoveRaycastSource(rayOrigin);
#endif
						m_MiniWorlds.rays.Remove(rayOrigin);
					}
				}
			}
		}
	}
}
#endif
