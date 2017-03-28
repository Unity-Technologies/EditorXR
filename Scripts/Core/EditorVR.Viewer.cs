#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		class Viewer : Nested, IInterfaceConnector
		{
			const float k_CameraRigTransitionTime = 0.75f;

			PlayerBody m_PlayerBody;

			public Viewer()
			{
				IMoveCameraRigMethods.moveCameraRig = MoveCameraRig;
				IUsesViewerBodyMethods.isOverShoulder = IsOverShoulder;
				IUsesViewerBodyMethods.isAboveHead = IsAboveHead;
				IUsesViewerScaleMethods.getViewerScale = GetViewerScale;
			}

			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				var locomotion = obj as ILocomotor;
				if (locomotion != null)
					locomotion.cameraRig = VRView.cameraRig;

				var usesCameraRig = obj as IUsesCameraRig;
				if (usesCameraRig != null)
					usesCameraRig.cameraRig = CameraUtils.GetCameraRig();
			}

			public void DisconnectInterface(object obj)
			{
			}

			internal void AddPlayerModel()
			{
				m_PlayerBody = ObjectUtils.Instantiate(evr.m_PlayerModelPrefab, CameraUtils.GetMainCamera().transform, false).GetComponent<PlayerBody>();
				var renderer = m_PlayerBody.GetComponent<Renderer>();
				evr.GetModule<SpatialHashModule>().spatialHash.AddObject(renderer, renderer.bounds);
			}

			internal bool IsOverShoulder(Transform rayOrigin)
			{
				return Overlaps(rayOrigin, m_PlayerBody.overShoulderTrigger);
			}

			bool IsAboveHead(Transform rayOrigin)
			{
				return Overlaps(rayOrigin, m_PlayerBody.aboveHeadTrigger);
			}

			static bool Overlaps(Transform rayOrigin, Collider trigger)
			{
				var radius = evr.GetNestedModule<DirectSelection>().GetPointerLength(rayOrigin);

				var colliders = Physics.OverlapSphere(rayOrigin.position, radius, -1, QueryTriggerInteraction.Collide);
				foreach (var collider in colliders)
				{
					if (collider == trigger)
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
					playerHead.hideFlags = defaultHideFlags;
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

			static void MoveCameraRig(Vector3 position, Vector3? viewdirection)
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
