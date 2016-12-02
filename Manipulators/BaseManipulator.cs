using System;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Manipulators
{
	public class BaseManipulator : MonoBehaviour, IManipulator
	{
		public bool dragging { get; protected set; }

		protected const float kBaseManipulatorSize = 0.5f;

		public Action<Vector3> translate { protected get; set; }
		public Action<Quaternion> rotate { protected get; set; }
		public Action<Vector3> scale { protected get; set; }

		protected virtual void OnEnable()
		{
			Camera.onPreRender += OnCameraPreRender;
		}

		protected virtual void OnDisable()
		{
			Camera.onPreRender -= OnCameraPreRender;
		}

		void OnCameraPreRender(Camera camera)
		{
			AdjustScale(camera.transform, camera.worldToCameraMatrix);
		}

		public void AdjustScale(Transform cameraTransform, Matrix4x4 worldToCameraMatrix)
		{
			// Adjust size of manipulator while accounting for any non-standard cameras (e.g. scaling applied to the camera)
			var manipulatorPosition = worldToCameraMatrix.MultiplyPoint3x4(transform.position);
			var cameraPosition = worldToCameraMatrix.MultiplyPoint3x4(cameraTransform.position);
			var delta = worldToCameraMatrix.inverse.MultiplyPoint3x4(cameraPosition - manipulatorPosition) - cameraTransform.position;
			transform.localScale = Vector3.one * delta.magnitude * kBaseManipulatorSize;
		}
	}
}