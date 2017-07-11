#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Manipulators
{
	class BaseManipulator : MonoBehaviour, IManipulator
	{
		protected const float k_BaseManipulatorSize = 0.3f;

		[SerializeField]
		protected List<BaseHandle> m_AllHandles;

		[SerializeField]
		float m_LinearHandleScaleBump = 1.3f;

		[SerializeField]
		float m_PlaneHandleScaleBump = 1.1f;

		[SerializeField]
		float m_SphereHandleScaleBump = 1.1f;

		public bool adjustScaleForCamera { get; set; }

		public Action<Vector3, Transform, ConstrainedAxis> translate { protected get; set; }
		public Action<Quaternion, Transform> rotate { protected get; set; }
		public Action<Vector3> scale { protected get; set; }

		public bool dragging { get; protected set; }
		public event Action dragStarted;
		public event Action<Transform> dragEnded;

		readonly Dictionary<Type, float> m_ScaleBumps = new Dictionary<Type, float>();

		void Awake()
		{
			m_ScaleBumps[typeof(LinearHandle)] = m_LinearHandleScaleBump;
			m_ScaleBumps[typeof(PlaneHandle)] = m_LinearHandleScaleBump;
			m_ScaleBumps[typeof(SphereHandle)] = m_LinearHandleScaleBump;
		}

		protected virtual void OnHandleHoverStarted(BaseHandle handle, HandleEventData eventData, float scaleBump)
		{
			handle.transform.localScale *= scaleBump;
		}

		protected virtual void OnHandleHovering(BaseHandle handle, HandleEventData eventData, float scaleBump)
		{

		}

		void OnHoverEnded(BaseHandle handle, HandleEventData eventData, float scaleBump)
		{
			handle.transform.localScale /= scaleBump;
		}

		protected virtual void OnEnable()
		{
			if (adjustScaleForCamera)
				Camera.onPreRender += OnCameraPreRender;

			foreach (var h in m_AllHandles) {
				SetUpHandle(h);
			}
		}

		protected virtual void SetUpHandle(BaseHandle handle)
		{
			handle.hoverStarted += 
		}

		protected virtual void TakeDownHandle(BaseHandle handle) {
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
