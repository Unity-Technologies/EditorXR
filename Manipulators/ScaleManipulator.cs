using System.Collections.Generic;
using UnityEngine.Experimental.EditorVR.Handles;

namespace UnityEngine.Experimental.EditorVR.Manipulators
{
	public class ScaleManipulator : BaseManipulator
	{
		[SerializeField]
		private BaseHandle m_UniformHandle;

		[SerializeField]
		private List<BaseHandle> m_AxesHandles;

		private readonly List<BaseHandle> m_AllHandles = new List<BaseHandle>();

		void Awake()
		{
			m_AllHandles.Add(m_UniformHandle);
			m_AllHandles.AddRange(m_AxesHandles);
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			m_UniformHandle.dragging += OnUniformScaleDragging;

			foreach (var h in m_AxesHandles)
				h.dragging += OnLinearScaleDragging;

			foreach (var h in m_AllHandles)
			{
				h.dragStarted += OnHandleDragStarted;
				h.dragEnded += OnHandleDragEnded;
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			m_UniformHandle.dragging -= OnUniformScaleDragging;

			foreach (var h in m_AxesHandles)
				h.dragging -= OnLinearScaleDragging;

			foreach (var h in m_AllHandles)
			{
				h.dragStarted -= OnHandleDragStarted;
				h.dragEnded -= OnHandleDragEnded;
			}
		}

		void OnLinearScaleDragging(BaseHandle handle, HandleEventData eventData)
		{
			float delta = handle.transform.InverseTransformVector(eventData.deltaPosition).z / handle.transform.InverseTransformPoint(handle.startDragPosition).z;
			scale(delta * transform.InverseTransformVector(handle.transform.forward));
		}

		void OnUniformScaleDragging(BaseHandle handle, HandleEventData eventData)
		{
			scale(Vector3.one * eventData.deltaPosition.y);
		}

		void OnHandleDragStarted(BaseHandle handle, HandleEventData eventData)
		{
			foreach (var h in m_AllHandles)
				h.gameObject.SetActive(h == handle);

			dragging = true;
		}

		private void OnHandleDragEnded(BaseHandle handle, HandleEventData eventData)
		{
			foreach (var h in m_AllHandles)
				h.gameObject.SetActive(true);

			dragging = false;
		}
	}
}