#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Manipulators
{
	class BaseManipulator : MonoBehaviour, IManipulator
	{
		protected const float k_BaseManipulatorSize = 0.3f;

		public bool adjustScaleForCamera { get; set; }

		public Action<Vector3, Transform, bool> translate { protected get; set; }
		public Action<Quaternion> rotate { protected get; set; }
		public Action<Vector3> scale { protected get; set; }

		public bool dragging { get; protected set; }
		public event Action dragStarted;
		public event Action<Transform> dragEnded;

		protected virtual void OnEnable()
		{
			if (adjustScaleForCamera)
				Camera.onPreRender += OnCameraPreRender;
		}

		protected virtual void OnDisable()
		{
			Camera.onPreRender -= OnCameraPreRender;
		}

		void OnCameraPreRender(Camera camera)
		{
			AdjustScale(camera.transform.position, camera.worldToCameraMatrix);
		}

		public void AdjustScale(Vector3 cameraPosition, Matrix4x4 worldToCameraMatrix)
		{
			var originalCameraPosition = cameraPosition;
			
			// Adjust size of manipulator while accounting for any non-standard cameras (e.g. scaling applied to the camera)
			var manipulatorPosition = worldToCameraMatrix.MultiplyPoint3x4(transform.position);
			cameraPosition = worldToCameraMatrix.MultiplyPoint3x4(cameraPosition);
			var delta = worldToCameraMatrix.inverse.MultiplyPoint3x4(cameraPosition - manipulatorPosition) - originalCameraPosition;
			transform.localScale = Vector3.one * delta.magnitude * k_BaseManipulatorSize;
		}

		protected void OnDragStarted()
		{
			if (dragStarted != null)
				dragStarted();
		}

		protected void OnDragEnded(Transform rayOrigin)
		{
			if (dragEnded != null)
				dragEnded(rayOrigin);
		}
	}
}
#endif
