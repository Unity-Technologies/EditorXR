#if UNITY_EDITORVR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		const float kViewerPivotTransitionTime = 0.75f;

		SpatialHashModule m_SpatialHashModule;
		IntersectionModule m_IntersectionModule;

		IGrabObjects m_ObjectsGrabber;

		// Local method use only -- created here to reduce garbage collection
		readonly Dictionary<Transform, DirectSelectionData> m_DirectSelectionResults = new Dictionary<Transform, DirectSelectionData>();
		readonly List<ActionMapInput> m_ActiveStates = new List<ActionMapInput>();

		void CreateSpatialSystem()
		{
			// Create event system, input module, and event camera
			m_SpatialHashModule = U.Object.AddComponent<SpatialHashModule>(gameObject);
			m_SpatialHashModule.shouldExcludeObject = go => go.GetComponentInParent<EditorVR>();
			m_SpatialHashModule.Setup();
			m_IntersectionModule = U.Object.AddComponent<IntersectionModule>(gameObject);
			ConnectInterfaces(m_IntersectionModule);
			m_IntersectionModule.Setup(m_SpatialHashModule.spatialHash);

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				var tester = rayOriginPair.Value.GetComponentInChildren<IntersectionTester>();
				tester.active = proxy.active;
				m_IntersectionModule.AddTester(tester);
			}, false);
		}

		// NOTE: This is for the length of the pointer object, not the length of the ray coming out of the pointer
		float GetPointerLength(Transform rayOrigin)
		{
			var length = 0f;

			// Check if this is a MiniWorldRay
			MiniWorldRay ray;
			if (m_MiniWorldRays.TryGetValue(rayOrigin, out ray))
				rayOrigin = ray.originalRayOrigin;

			DefaultProxyRay dpr;
			if (m_DefaultRays.TryGetValue(rayOrigin, out dpr))
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

		Dictionary<Transform, DirectSelectionData> GetDirectSelection()
		{
			m_DirectSelectionResults.Clear();
			m_ActiveStates.Clear();

			var directSelection = m_ObjectsGrabber;
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				var rayOrigin = rayOriginPair.Value;
				var input = deviceData.directSelectInput;
				var obj = GetDirectSelectionForRayOrigin(rayOrigin, input);
				if (obj && !obj.CompareTag(kVRPlayerTag))
				{
					m_ActiveStates.Add(input);
					m_DirectSelectionResults[rayOrigin] = new DirectSelectionData
					{
						gameObject = obj,
						node = rayOriginPair.Key,
						input = input
					};
				}
				else if (directSelection != null && directSelection.GetHeldObjects(rayOrigin) != null)
				{
					m_ActiveStates.Add(input);
				}
			});

			foreach (var ray in m_MiniWorldRays)
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
			ForEachRayOrigin((proxy, pair, device, deviceData) =>
			{
				var input = deviceData.directSelectInput;
				input.active = m_ActiveStates.Contains(input);
			});

			return m_DirectSelectionResults;
		}

		GameObject GetDirectSelectionForRayOrigin(Transform rayOrigin, ActionMapInput input)
		{
			if (m_IntersectionModule)
			{
				var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();

				var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester);
				if (renderer)
					return renderer.gameObject;
			}
			return null;
		}

		bool CanGrabObject(GameObject selection, Transform rayOrigin)
		{
			if (selection.CompareTag(kVRPlayerTag) && !m_MiniWorldRays.ContainsKey(rayOrigin))
				return false;

			return true;
		}

		static void OnObjectGrabbed(GameObject selection)
		{
			// Detach the player head model so that it is not affected by its parent transform
			if (selection.CompareTag(kVRPlayerTag))
				selection.transform.parent = null;
		}

		void OnObjectsDropped(Transform[] grabbedObjects, Transform rayOrigin)
		{
			foreach (var grabbedObject in grabbedObjects)
			{
				// Dropping the player head updates the viewer pivot
				if (grabbedObject.CompareTag(kVRPlayerTag))
					StartCoroutine(UpdateViewerPivot(grabbedObject));
				else if (IsOverShoulder(rayOrigin) && !m_MiniWorldRays.ContainsKey(rayOrigin))
					DeleteSceneObject(grabbedObject.gameObject);
			}
		}

		static IEnumerator UpdateViewerPivot(Transform playerHead)
		{
			var viewerPivot = U.Camera.GetViewerPivot();

			// Hide player head to avoid jarring impact
			var playerHeadRenderers = playerHead.GetComponentsInChildren<Renderer>();
			foreach (var renderer in playerHeadRenderers)
			{
				renderer.enabled = false;
			}

			var mainCamera = U.Camera.GetMainCamera().transform;
			var startPosition = viewerPivot.position;
			var startRotation = viewerPivot.rotation;

			var rotationDiff = U.Math.ConstrainYawRotation(Quaternion.Inverse(mainCamera.rotation) * playerHead.rotation);
			var cameraDiff = viewerPivot.position - mainCamera.position;
			cameraDiff.y = 0;
			var rotationOffset = rotationDiff * cameraDiff - cameraDiff;

			var endPosition = viewerPivot.position + (playerHead.position - mainCamera.position) + rotationOffset;
			var endRotation = viewerPivot.rotation * rotationDiff;
			var startTime = Time.realtimeSinceStartup;
			var diffTime = 0f;

			while (diffTime < kViewerPivotTransitionTime)
			{
				diffTime = Time.realtimeSinceStartup - startTime;
				var t = diffTime / kViewerPivotTransitionTime;

				// Use a Lerp instead of SmoothDamp for constant velocity (avoid motion sickness)
				viewerPivot.position = Vector3.Lerp(startPosition, endPosition, t);
				viewerPivot.rotation = Quaternion.Lerp(startRotation, endRotation, t);
				yield return null;
			}

			viewerPivot.position = endPosition;
			viewerPivot.rotation = endRotation;

			playerHead.parent = mainCamera;
			playerHead.localRotation = Quaternion.identity;
			playerHead.localPosition = Vector3.zero;

			foreach (var renderer in playerHeadRenderers)
			{
				renderer.enabled = true;
			}
		}
	}
}
#endif
