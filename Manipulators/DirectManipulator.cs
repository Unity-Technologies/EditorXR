using System;
using System.Collections.Generic;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR.Manipulators
{
	public class DirectManipulator : MonoBehaviour, IManipulator
	{
		public Transform target
		{
			set { m_Target = value; }
		}

		[SerializeField]
		private Transform m_Target;

		[SerializeField]
		private List<BaseHandle> m_AllHandles = new List<BaseHandle>();

		public bool dragging { get; private set; }

		private Vector3 m_PositionOffset;
		private Quaternion m_RotationOffset;

		public Action<Vector3> translate { private get; set; }
		public Action<Quaternion> rotate { private get; set; }
		public Action<Vector3> scale { private get; set; }

		void OnEnable()
		{
			foreach (var h in m_AllHandles)
			{
				h.dragStarted += OnHandleDragStarted;
				h.dragging += OnHandleDragging;
				h.dragEnded += OnHandleDragEnded;
			}
		}

		void OnDisable()
		{
			foreach (var h in m_AllHandles)
			{
				h.dragStarted -= OnHandleDragStarted;
				h.dragging -= OnHandleDragging;
				h.dragEnded -= OnHandleDragEnded;
			}
		}

		private void OnHandleDragStarted(BaseHandle handle, HandleEventData eventData)
		{
			foreach (var h in m_AllHandles)
			{
				h.gameObject.SetActive(h == handle);
			}
			dragging = true;

			var target = m_Target == null ? transform : m_Target;

			var rayOrigin = eventData.rayOrigin;
			var inverseRotation = Quaternion.Inverse(rayOrigin.rotation);
			m_PositionOffset = inverseRotation * (target.transform.position - rayOrigin.position);
			m_RotationOffset = inverseRotation * target.transform.rotation;
		}

		private void OnHandleDragging(BaseHandle handle, HandleEventData eventData)
		{
			var target = m_Target == null ? transform : m_Target;

			var rayOrigin = eventData.rayOrigin;
			translate(rayOrigin.position + rayOrigin.rotation * m_PositionOffset - target.position);
			rotate(Quaternion.Inverse(target.rotation) * rayOrigin.rotation * m_RotationOffset);
		}

		private void OnHandleDragEnded(BaseHandle handle, HandleEventData eventData)
		{
			if (gameObject.activeSelf)
			{
				foreach (var h in m_AllHandles)
				{
					h.gameObject.SetActive(true);
				}
			}

			dragging = false;
		}
	}
}