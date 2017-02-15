#if UNITY_EDITORVR
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR : MonoBehaviour
	{
		class Viewer : Nested
		{
			const float kCameraRigTransitionTime = 0.75f;

			internal void AddPlayerModel()
			{
				var playerModel = U.Object.Instantiate(evr.m_PlayerModelPrefab, U.Camera.GetMainCamera().transform, false).GetComponent<Renderer>();
				evr.m_SpatialHashModule.spatialHash.AddObject(playerModel, playerModel.bounds);
			}

			internal bool IsOverShoulder(Transform rayOrigin)
			{
				var radius = evr.m_DirectSelection.GetPointerLength(rayOrigin);
				var colliders = Physics.OverlapSphere(rayOrigin.position, radius, -1, QueryTriggerInteraction.Collide);
				foreach (var collider in colliders)
				{
					if (collider.CompareTag(kVRPlayerTag))
						return true;
				}
				return false;
			}

			internal static IEnumerator MoveCameraRig(Transform playerHead)
			{
				var rig = U.Camera.GetCameraRig();

				// Hide player head to avoid jarring impact
				var playerHeadRenderers = playerHead.GetComponentsInChildren<Renderer>();
				foreach (var renderer in playerHeadRenderers)
				{
					renderer.enabled = false;
				}

				var mainCamera = U.Camera.GetMainCamera().transform;
				var startPosition = rig.position;
				var startRotation = rig.rotation;

				var rotationDiff = U.Math.ConstrainYawRotation(Quaternion.Inverse(mainCamera.rotation) * playerHead.rotation);
				var cameraDiff = rig.position - mainCamera.position;
				cameraDiff.y = 0;
				var rotationOffset = rotationDiff * cameraDiff - cameraDiff;

				var endPosition = rig.position + (playerHead.position - mainCamera.position) + rotationOffset;
				var endRotation = rig.rotation * rotationDiff;
				var startTime = Time.realtimeSinceStartup;
				var diffTime = 0f;

				while (diffTime < kCameraRigTransitionTime)
				{
					diffTime = Time.realtimeSinceStartup - startTime;
					var t = diffTime / kCameraRigTransitionTime;

					// Use a Lerp instead of SmoothDamp for constant velocity (avoid motion sickness)
					rig.position = Vector3.Lerp(startPosition, endPosition, t);
					rig.rotation = Quaternion.Lerp(startRotation, endRotation, t);
					yield return null;
				}

				rig.position = endPosition;
				rig.rotation = endRotation;

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
}
#endif
