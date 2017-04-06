#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.VR;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		[SerializeField]
		GameObject m_PreviewCameraPrefab;

		class Viewer : Nested, IInterfaceConnector, ISerializePreferences
		{
			[Serializable]
			class Preferences
			{
				[SerializeField]
				Vector3 m_CameraRigPosition;
				[SerializeField]
				Quaternion m_CameraRigRotation;

				public Vector3 cameraRigPosition { get { return m_CameraRigPosition; } set { m_CameraRigPosition = value; } }
				public Quaternion cameraRigRotation { get { return m_CameraRigRotation; } set { m_CameraRigRotation = value; } }
			}

			const float k_CameraRigTransitionTime = 0.75f;

			PlayerBody m_PlayerBody;

			internal IPreviewCamera customPreviewCamera { get; private set; }

			public bool preserveCameraRig { private get; set; }

			public Viewer()
			{
				IMoveCameraRigMethods.moveCameraRig = MoveCameraRig;
				IUsesViewerBodyMethods.isOverShoulder = IsOverShoulder;
				IUsesViewerBodyMethods.isAboveHead = IsAboveHead;
				IUsesViewerScaleMethods.getViewerScale = GetViewerScale;

				preserveCameraRig = true;
			}

			internal override void OnDestroy()
			{
				base.OnDestroy();

				if (customPreviewCamera != null)
					ObjectUtils.Destroy(((MonoBehaviour)customPreviewCamera).gameObject);
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

			public object OnSerializePreferences()
			{
				if (!preserveCameraRig)
					return null;

				var cameraRig = CameraUtils.GetCameraRig();

				var preferences = new Preferences();
				preferences.cameraRigPosition = cameraRig.position;
				preferences.cameraRigRotation = cameraRig.rotation;

				return preferences;
			}

			public void OnDeserializePreferences(object obj)
			{
				if (!preserveCameraRig)
					return;

				var preferences = (Preferences)obj;

				var cameraRig = CameraUtils.GetCameraRig();
				cameraRig.position = preferences.cameraRigPosition;
				cameraRig.rotation = preferences.cameraRigRotation;
			}

			internal void InitializeCamera()
			{
				var cameraRig = CameraUtils.GetCameraRig();
				cameraRig.parent = evr.transform; // Parent the camera rig under EditorVR
				cameraRig.hideFlags = defaultHideFlags;
				var viewerCamera = CameraUtils.GetMainCamera();
				viewerCamera.gameObject.hideFlags = defaultHideFlags;
				if (VRSettings.loadedDeviceName == "OpenVR")
				{
					// Steam's reference position should be at the feet and not at the head as we do with Oculus
					cameraRig.localPosition = Vector3.zero;
				}

				var hmdOnlyLayerMask = 0;
				if (evr.m_PreviewCameraPrefab)
				{
					var go = ObjectUtils.Instantiate(evr.m_PreviewCameraPrefab);

					customPreviewCamera = go.GetComponentInChildren<IPreviewCamera>();
					if (customPreviewCamera != null)
					{
						VRView.customPreviewCamera = customPreviewCamera.previewCamera;
						customPreviewCamera.vrCamera = VRView.viewerCamera;
						hmdOnlyLayerMask = customPreviewCamera.hmdOnlyLayerMask;
						evr.m_Interfaces.ConnectInterfaces(customPreviewCamera);
					}
				}
				VRView.cullingMask = UnityEditor.Tools.visibleLayers | hmdOnlyLayerMask;
			}

			internal void UpdateCamera()
			{
				if (customPreviewCamera != null)
					customPreviewCamera.enabled = VRView.showDeviceView && VRView.customPreviewCamera != null;
			}

			internal void AddPlayerModel()
			{
				m_PlayerBody = ObjectUtils.Instantiate(evr.m_PlayerModelPrefab, CameraUtils.GetMainCamera().transform, false).GetComponent<PlayerBody>();
				var renderer = m_PlayerBody.GetComponent<Renderer>();
				evr.GetModule<SpatialHashModule>().spatialHash.AddObject(renderer, renderer.bounds);
				evr.GetModule<SnappingModule>().ignoreList = renderer.GetComponentsInChildren<Renderer>(true);
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
				var radius = DirectSelection.GetPointerLength(rayOrigin);

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
