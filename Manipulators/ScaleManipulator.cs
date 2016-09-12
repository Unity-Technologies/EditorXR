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
	private List<BaseHandle> m_AxesHandles;
	
	private readonly List<BaseHandle> m_AllHandles = new List<BaseHandle>();

	public bool dragging { get { return m_Dragging; } }
	private bool m_Dragging;

	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }
	public Action<Vector3> scale { private get; set; }

	void Awake()
	{
		m_AllHandles.Add(m_UniformHandle);
		m_AllHandles.AddRange(m_AxesHandles);
	}

	void OnEnable()
	{
		m_UniformHandle.handleDrag += UniformScaleHandleDrag;

		foreach (var h in m_AxesHandles)
			h.handleDrag += LinearScaleHandleDrag;

		foreach (var h in m_AllHandles)
		{
			h.handleDragging += HandleDragging;
			h.handleDragged += HandleDragged;
		}
	}

	void OnDisable()
	{
		m_UniformHandle.handleDrag -= UniformScaleHandleDrag;

		foreach (var h in m_AxesHandles)
			h.handleDrag -= LinearScaleHandleDrag;

		foreach (var h in m_AllHandles)
		{
			h.handleDragging -= HandleDragging;
			h.handleDragged -= HandleDragged;
		}
	}

	private void LinearScaleHandleDrag(BaseHandle handle, HandleEventData eventData)
	{
		float delta = handle.transform.InverseTransformVector(eventData.deltaPosition).z / handle.transform.InverseTransformPoint(handle.startDragPosition).z;
		scale(delta * transform.InverseTransformVector(handle.transform.forward));
	}

	private void UniformScaleHandleDrag(BaseHandle handle, HandleEventData eventData)
	{
		scale(Vector3.one * eventData.deltaPosition.y);
	}

	private void HandleDragging(BaseHandle handle, HandleEventData eventData)
	{
		foreach (var h in m_AllHandles)
			h.gameObject.SetActive(h == handle);

		m_Dragging = true;
	}

	private void HandleDragged(BaseHandle handle, HandleEventData eventData)
	{
		foreach (var h in m_AllHandles)
			h.gameObject.SetActive(true);

		m_Dragging = false;
	}
}