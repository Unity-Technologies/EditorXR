#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Manipulators {
	sealed class StandardManipulator : BaseManipulator
	{
		[SerializeField]
		Transform m_PlaneHandlesParent;

		protected override void SetUpHandle(BaseHandle handle)
		{
			base.SetUpHandle(handle);
			if (handle is LinearHandle || handle is PlaneHandle || handle is SphereHandle)
				handle.dragging += OnTranslateDragging;

			if (handle is RadialHandle)
				handle.dragging += OnRotateDragging;

			handle.dragStarted += OnHandleDragStarted;
			handle.dragEnded += OnHandleDragEnded;
		}

		protected override void TakeDownHandle(BaseHandle handle)
		{
			base.TakeDownHandle(handle);
			if (handle is LinearHandle || handle is PlaneHandle || handle is SphereHandle)
				handle.dragging -= OnTranslateDragging;

			if (handle is RadialHandle)
				handle.dragging -= OnRotateDragging;

			handle.dragStarted -= OnHandleDragStarted;
			handle.dragEnded -= OnHandleDragEnded;
		}

		void Update()
		{
			if (!dragging)
			{
				// Place the plane handles in a good location that is accessible to the user
				var viewerPosition = CameraUtils.GetMainCamera().transform.position;
				foreach (Transform t in m_PlaneHandlesParent)
				{
					var localPos = t.localPosition;
					localPos.x = Mathf.Abs(localPos.x) * (transform.position.x < viewerPosition.x ? 1 : -1);
					localPos.y = Mathf.Abs(localPos.y) * (transform.position.y < viewerPosition.y ? 1 : -1);
					localPos.z = Mathf.Abs(localPos.z) * (transform.position.z < viewerPosition.z ? 1 : -1);
					t.localPosition = localPos;
				}
			}
		}

		void OnTranslateDragging(BaseHandle handle, HandleEventData eventData)
		{
			ConstrainedAxis constraints = 0;
			var constrainedHandle = handle as IAxisConstraints;
			if (constrainedHandle != null)
				constraints = constrainedHandle.constraints;

			translate(eventData.deltaPosition, eventData.rayOrigin, constraints);
		}

		void OnRotateDragging(BaseHandle handle, HandleEventData eventData)
		{
			rotate(eventData.deltaRotation, eventData.rayOrigin);
		}

		void OnHandleDragStarted(BaseHandle handle, HandleEventData eventData)
		{
			foreach (var h in m_AllHandles)
				h.gameObject.SetActive(h == handle);

			OnDragStarted();

			dragging = true;
		}

		void OnHandleDragEnded(BaseHandle handle, HandleEventData eventData)
		{
			if (gameObject.activeSelf)
				foreach (var h in m_AllHandles)
					h.gameObject.SetActive(true);

			OnDragEnded(eventData.rayOrigin);

			dragging = false;
		}
	}
}
#endif
