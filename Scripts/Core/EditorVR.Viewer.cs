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
	partial class EditorVR : MonoBehaviour
	{
		const float kViewerPivotTransitionTime = 0.75f;

		void AddPlayerModel()
		{
			var playerModel = U.Object.Instantiate(m_PlayerModelPrefab, U.Camera.GetMainCamera().transform, false).GetComponent<Renderer>();
			m_SpatialHashModule.spatialHash.AddObject(playerModel, playerModel.bounds);
		}

		bool IsOverShoulder(Transform rayOrigin)
		{
			var radius = m_DirectSelection.GetPointerLength(rayOrigin);
			var colliders = Physics.OverlapSphere(rayOrigin.position, radius, -1, QueryTriggerInteraction.Collide);
			foreach (var collider in colliders)
			{
				if (collider.CompareTag(kVRPlayerTag))
					return true;
			}
			return false;
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
