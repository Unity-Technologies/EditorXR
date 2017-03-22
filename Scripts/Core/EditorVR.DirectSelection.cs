#if UNITY_EDITOR && UNITY_EDITORVR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class DirectSelection : Nested, IInterfaceConnector
		{
			internal IGrabObjects objectsGrabber { get; set; }

			internal Dictionary<Transform, DirectSelectionData> directSelections { get { return m_DirectSelections; } }
			readonly Dictionary<Transform, DirectSelectionData> m_DirectSelections = new Dictionary<Transform, DirectSelectionData>();

			// Local method use only -- created here to reduce garbage collection
			readonly List<ActionMapInput> m_ActiveStates = new List<ActionMapInput>();

			public DirectSelection()
			{
				IUsesDirectSelectionMethods.getDirectSelection = () => directSelections;

				IGrabObjectsMethods.canGrabObject = CanGrabObject;
			}

			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var grabObjects = obj as IGrabObjects;
				if (grabObjects != null)
				{
					grabObjects.objectGrabbed += OnObjectGrabbed;
					grabObjects.objectsDropped += OnObjectsDropped;
				}
			}

			public void DisconnectInterface(object obj)
			{
				var grabObjects = obj as IGrabObjects;
				if (grabObjects != null)
				{
					grabObjects.objectGrabbed -= OnObjectGrabbed;
					grabObjects.objectsDropped -= OnObjectsDropped;
				}
			}

			// NOTE: This is for the length of the pointer object, not the length of the ray coming out of the pointer
			internal float GetPointerLength(Transform rayOrigin)
			{
				var length = 0f;

				// Check if this is a MiniWorldRay
				MiniWorlds.MiniWorldRay ray;
				if (evr.m_MiniWorlds.rays.TryGetValue(rayOrigin, out ray))
					rayOrigin = ray.originalRayOrigin;

				DefaultProxyRay dpr;
				if (evr.m_Rays.defaultRays.TryGetValue(rayOrigin, out dpr))
				{
					length = dpr.pointerLength;

					// If this is a MiniWorldRay, scale the pointer length to the correct size relative to MiniWorld objects
					if (ray != null)
					{
						var miniWorld = ray.miniWorld;

						// As the miniworld gets smaller, the ray length grows, hence lossyScale.Inverse().
						// Assume that both transforms have uniform scale, so we just need .x
						length *= miniWorld.referenceTransform.TransformVector(miniWorld.miniWorldTransform.lossyScale.Inverse()).x;
					}
				}

				return length;
			}

			internal void UpdateDirectSelection()
			{
				m_DirectSelections.Clear();
				m_ActiveStates.Clear();

				var directSelection = objectsGrabber;
				var evrRays = evr.m_Rays;
				evrRays.ForEachProxyDevice((deviceData) =>
				{
					var rayOrigin = deviceData.rayOrigin;
					var input = deviceData.directSelectInput;
					var obj = GetDirectSelectionForRayOrigin(rayOrigin, input);
					if (obj && !obj.CompareTag(k_VRPlayerTag))
					{
						m_ActiveStates.Add(input);
						m_DirectSelections[rayOrigin] = new DirectSelectionData
						{
							gameObject = obj,
							node = deviceData.node,
							input = input
						};
					}
					else if (directSelection != null && directSelection.GetHeldObjects(rayOrigin) != null)
					{
						m_ActiveStates.Add(input);
					}
				});

				foreach (var ray in evr.m_MiniWorlds.rays)
				{
					var rayOrigin = ray.Key;
					var miniWorldRay = ray.Value;
					var input = miniWorldRay.directSelectInput;
					var go = GetDirectSelectionForRayOrigin(rayOrigin, input);
					if (go != null)
					{
						m_ActiveStates.Add(input);
						m_DirectSelections[rayOrigin] = new DirectSelectionData
						{
							gameObject = go,
							node = ray.Value.node,
							input = input
						};
					}
					else if (miniWorldRay.dragObjects != null
						|| (directSelection != null && directSelection.GetHeldObjects(rayOrigin) != null))
					{
						m_ActiveStates.Add(input);
					}
				}

				// Only activate direct selection input if the cone is inside of an object, so a trigger press can be detected,
				// and keep it active if we are dragging
				evrRays.ForEachProxyDevice((deviceData) =>
				{
					var input = deviceData.directSelectInput;
					input.active = m_ActiveStates.Contains(input);
				});
			}

			GameObject GetDirectSelectionForRayOrigin(Transform rayOrigin, ActionMapInput input)
			{
				var intersectionModule = evr.m_IntersectionModule;
				if (intersectionModule)
				{
					var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();

					var renderer = intersectionModule.GetIntersectedObjectForTester(tester);
					if (renderer)
						return renderer.gameObject;
				}
				return null;
			}

			internal bool CanGrabObject(GameObject selection, Transform rayOrigin)
			{
				if (selection.CompareTag(k_VRPlayerTag) && !evr.m_MiniWorlds.rays.ContainsKey(rayOrigin))
					return false;

				return true;
			}

			internal void OnObjectGrabbed(GameObject selection)
			{
				// Detach the player head model so that it is not affected by its parent transform
				if (selection.CompareTag(k_VRPlayerTag))
				{
					selection.hideFlags = HideFlags.None;
					selection.transform.parent = null;
				}
			}

			internal void OnObjectsDropped(Transform[] grabbedObjects, Transform rayOrigin)
			{
				foreach (var grabbedObject in grabbedObjects)
				{
					// Dropping the player head updates the camera rig position
					if (grabbedObject.CompareTag(k_VRPlayerTag))
						Viewer.DropPlayerHead(grabbedObject);
					else if (evr.m_Viewer.IsOverShoulder(rayOrigin) && !evr.m_MiniWorlds.rays.ContainsKey(rayOrigin))
						evr.m_SceneObjectModule.DeleteSceneObject(grabbedObject.gameObject);
				}
			}
		}
	}
}
#endif
