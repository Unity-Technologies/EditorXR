#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Manipulators
{
	sealed class StandardManipulator : BaseManipulator
	{
		[SerializeField]
		Transform m_PlaneHandlesParent;

		[SerializeField]
		List<BaseHandle> m_AllHandles;

		protected override void OnEnable()
		{
			base.OnEnable();

			foreach (var h in m_AllHandles)
			{
				if (h is LinearHandle || h is PlaneHandle || h is SphereHandle)
					h.dragging += OnTranslateDragging;

				if (h is RadialHandle)
					h.dragging += OnRotateDragging;

				h.dragStarted += OnHandleDragStarted;
				h.dragEnded += OnHandleDragEnded;
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			foreach (var h in m_AllHandles)
			{
				if (h is LinearHandle || h is PlaneHandle || h is SphereHandle)
					h.dragging -= OnTranslateDragging;

				if (h is RadialHandle)
					h.dragging -= OnRotateDragging;

				h.dragStarted -= OnHandleDragStarted;
				h.dragEnded -= OnHandleDragEnded;
			}
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
			translate(eventData.deltaPosition, eventData.rayOrigin, !(handle is SphereHandle));
		}

		void OnRotateDragging(BaseHandle handle, HandleEventData eventData)
		{
			rotate(eventData.deltaRotation);
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
