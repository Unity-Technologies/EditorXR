#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		class Viewer : Nested
		{
			const float k_CameraRigTransitionTime = 0.75f;

			internal void AddPlayerModel()
			{
				var playerModel = ObjectUtils.Instantiate(evr.m_PlayerModelPrefab, CameraUtils.GetMainCamera().transform, false).GetComponent<Renderer>();
				evr.m_SpatialHashModule.spatialHash.AddObject(playerModel, playerModel.bounds);
			}

			internal bool IsOverShoulder(Transform rayOrigin)
			{
				var radius = evr.m_DirectSelection.GetPointerLength(rayOrigin);
				var colliders = Physics.OverlapSphere(rayOrigin.position, radius, -1, QueryTriggerInteraction.Collide);
				foreach (var collider in colliders)
				{
					if (collider.CompareTag(k_VRPlayerTag))
						return true;
				}
				return false;
			}

			internal static void DropPlayerHead(Transform playerHead)
			{
				var cameraRig = CameraUtils.GetCameraRig();
				var mainCamera = CameraUtils.GetMainCamera().transform;

				// Hide player head to avoid jarring impact
				var playerHeadRenderers = playerHead.GetComponentsInChildren<Renderer>();
				foreach (var renderer in playerHeadRenderers)
				{
					renderer.enabled = false;
				}

				var rotationDiff = MathUtilsExt.ConstrainYawRotation(Quaternion.Inverse(mainCamera.rotation) * playerHead.rotation);
				var cameraDiff = cameraRig.position - mainCamera.position;
				cameraDiff.y = 0;
				var rotationOffset = rotationDiff * cameraDiff - cameraDiff;

				var endPosition = cameraRig.position + (playerHead.position - mainCamera.position) + rotationOffset;
				var endRotation = cameraRig.rotation * rotationDiff;
				var viewDirection = endRotation * Vector3.forward;

				evr.StartCoroutine(UpdateCameraRig(endPosition, viewDirection, () =>
				{
					playerHead.parent = mainCamera;
					playerHead.localRotation = Quaternion.identity;
					playerHead.localPosition = Vector3.zero;

					foreach (var renderer in playerHeadRenderers)
					{
						renderer.enabled = true;
					}
				}));
			}

			static IEnumerator UpdateCameraRig(Vector3 position, Vector3? viewDirection, Action onComplete = null)
			{
				var cameraRig = CameraUtils.GetCameraRig();

				var startPosition = cameraRig.position;
				var startRotation = cameraRig.rotation;

				var rotation = startRotation;
				if (viewDirection.HasValue)
				{
					var direction = viewDirection.Value;
					direction.y = 0;
					rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
				}

				var diffTime = 0f;
				var startTime = Time.realtimeSinceStartup;
				while (diffTime < k_CameraRigTransitionTime)
				{
					var t = diffTime / k_CameraRigTransitionTime;
					// Use a Lerp instead of SmoothDamp for constant velocity (avoid motion sickness)
					cameraRig.position = Vector3.Lerp(startPosition, position, t);
					cameraRig.rotation = Quaternion.Lerp(startRotation, rotation, t);
					yield return null;
					diffTime = Time.realtimeSinceStartup - startTime;
				}

				cameraRig.position = position;
				cameraRig.rotation = rotation;

				if (onComplete != null)
					onComplete();
			}

			internal static void MoveCameraRig(Vector3 position, Vector3? viewdirection)
			{
				evr.StartCoroutine(UpdateCameraRig(position, viewdirection));
			}

			internal static float GetViewerScale()
			{
				return CameraUtils.GetCameraRig().localScale.x;
			}
		}
	}
}
#endif
