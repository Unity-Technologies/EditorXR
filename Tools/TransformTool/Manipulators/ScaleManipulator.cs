using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;

public class ScaleManipulator : MonoBehaviour, IManipulator
{
	[SerializeField]
	private BaseHandle m_UniformHandle;
	[SerializeField]
	private BaseHandle m_HandleX;
	[SerializeField]
	private BaseHandle m_HandleY;
	[SerializeField]
	private BaseHandle m_HandleZ;

	private readonly List<BaseHandle> m_AllHandles = new List<BaseHandle>();
	private bool m_Dragging = false;

	public bool dragging { get { return m_Dragging; } }
	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }
	public Action<Vector3> scale { private get; set; }


	void Awake()
	{
		m_AllHandles.Add(m_UniformHandle);
		m_AllHandles.Add(m_HandleX);
		m_AllHandles.Add(m_HandleY);
		m_AllHandles.Add(m_HandleZ);
	}
	void OnEnable()
	{
		m_UniformHandle.onHandleDrag += UniformScaleHandleOnDrag;
		m_HandleX.onHandleDrag += LinearScaleHandleOnDrag;
		m_HandleY.onHandleDrag += LinearScaleHandleOnDrag;
		m_HandleZ.onHandleDrag += LinearScaleHandleOnDrag;

		foreach (var handle in m_AllHandles)
		{
			handle.onHandleBeginDrag += HandleOnBeginDrag;
			handle.onHandleEndDrag += HandleOnEndDrag;
		}
	}

	void OnDisable()
	{
		m_UniformHandle.onHandleDrag -= UniformScaleHandleOnDrag;

		foreach (var handle in m_AllHandles)
		{
			handle.onHandleBeginDrag -= HandleOnBeginDrag;
			handle.onHandleEndDrag -= HandleOnEndDrag;
		}
	}

	private void LinearScaleHandleOnDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		float delta = handle.transform.InverseTransformVector(eventData.deltaPosition).z / handle.transform.InverseTransformPoint(handle.startDragPosition).z;
		scale(delta * transform.InverseTransformVector(handle.transform.forward));
	}

	private void UniformScaleHandleOnDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		scale(Vector3.one * eventData.deltaPosition.y);
	}

	private void HandleOnBeginDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		SetAllHandlesActive(false);
		handle.gameObject.SetActive(true);
		m_Dragging = true;
	}

	private void HandleOnEndDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		SetAllHandlesActive(true);
		m_Dragging = false;
	}

	private void SetAllHandlesActive(bool active)
	{
		foreach (var handle in m_AllHandles)
			handle.gameObject.SetActive(active);
	}
}
