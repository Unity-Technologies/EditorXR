#if UNITY_EDITORVR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		class DirectSelection : Nested
		{
			internal IGrabObjects objectsGrabber { get; set; }

			// Local method use only -- created here to reduce garbage collection
			readonly Dictionary<Transform, DirectSelectionData> m_DirectSelectionResults = new Dictionary<Transform, DirectSelectionData>();
			readonly List<ActionMapInput> m_ActiveStates = new List<ActionMapInput>();

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

						// As the miniworld gets smaller, the ray length grows, hence localScale.Inverse().
						// Assume that both transforms have uniform scale, so we just need .x
						length *= miniWorld.referenceTransform.TransformVector(miniWorld.miniWorldTransform.localScale.Inverse()).x;
					}
				}

				return length;
			}

			internal Dictionary<Transform, DirectSelectionData> GetDirectSelection()
			{
				m_DirectSelectionResults.Clear();
				m_ActiveStates.Clear();

				var directSelection = objectsGrabber;
				var evrRays = evr.m_Rays;
				evrRays.ForEachProxyDevice((deviceData) =>
				{
					var rayOrigin = deviceData.rayOrigin;
					var input = deviceData.directSelectInput;
					var obj = GetDirectSelectionForRayOrigin(rayOrigin, input);
					if (obj && !obj.CompareTag(kVRPlayerTag))
					{
						m_ActiveStates.Add(input);
						m_DirectSelectionResults[rayOrigin] = new DirectSelectionData
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
						m_DirectSelectionResults[rayOrigin] = new DirectSelectionData
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

				return m_DirectSelectionResults;
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
				if (selection.CompareTag(kVRPlayerTag) && !evr.m_MiniWorlds.rays.ContainsKey(rayOrigin))
					return false;

				return true;
			}

			internal void OnObjectGrabbed(GameObject selection)
			{
				// Detach the player head model so that it is not affected by its parent transform
				if (selection.CompareTag(kVRPlayerTag))
					selection.transform.parent = null;
			}

			internal void OnObjectsDropped(Transform[] grabbedObjects, Transform rayOrigin)
			{
				foreach (var grabbedObject in grabbedObjects)
				{
					// Dropping the player head updates the camera rig position
					if (grabbedObject.CompareTag(kVRPlayerTag))
						Viewer.DropPlayerHead(grabbedObject);
					else if (evr.m_Viewer.IsOverShoulder(rayOrigin) && !evr.m_MiniWorlds.rays.ContainsKey(rayOrigin))
						evr.m_SceneObjectModule.DeleteSceneObject(grabbedObject.gameObject);
				}
			}
		}
	}
}
#endif
