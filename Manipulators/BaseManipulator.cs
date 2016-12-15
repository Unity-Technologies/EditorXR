using System;
using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR.Manipulators
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
			AdjustScale(camera.transform.position, camera.worldToCameraMatrix);
		}

		public void AdjustScale(Vector3 cameraPosition, Matrix4x4 worldToCameraMatrix)
		{
			var originalCameraPosition = cameraPosition;
			
			// Adjust size of manipulator while accounting for any non-standard cameras (e.g. scaling applied to the camera)
			var manipulatorPosition = worldToCameraMatrix.MultiplyPoint3x4(transform.position);
			cameraPosition = worldToCameraMatrix.MultiplyPoint3x4(cameraPosition);
			var delta = worldToCameraMatrix.inverse.MultiplyPoint3x4(cameraPosition - manipulatorPosition) - originalCameraPosition;
			transform.localScale = Vector3.one * delta.magnitude * kBaseManipulatorSize;
		}
	}
}