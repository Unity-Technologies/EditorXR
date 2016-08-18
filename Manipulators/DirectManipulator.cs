using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;

public class DirectManipulator : MonoBehaviour, IManipulator
{
	[SerializeField]
	private List<BaseHandle> m_AllHandles = new List<BaseHandle>();

	public bool dragging { get {  return m_Dragging; } }

	private bool m_Dragging;

	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }
	public Action<Vector3> scale { private get; set; }

	void OnEnable()
	{
		foreach (var h in m_AllHandles)
		{
			h.onHandleDrag += TranslateHandleOnDrag;
			h.onHandleBeginDrag += HandleOnBeginDrag;
			h.onHandleEndDrag += HandleOnEndDrag;
		}
	}

	void OnDisable()
	{
		foreach (var h in m_AllHandles)
		{
			h.onHandleDrag -= TranslateHandleOnDrag;
			h.onHandleBeginDrag -= HandleOnBeginDrag;
			h.onHandleEndDrag -= HandleOnEndDrag;
		}
	}

	private void TranslateHandleOnDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		translate(eventData.deltaPosition);
	}

	private void HandleOnBeginDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		foreach (var h in m_AllHandles)
			h.gameObject.SetActive(h == handle);

		m_Dragging = true;
	}

	private void HandleOnEndDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		foreach (var h in m_AllHandles)
			h.gameObject.SetActive(true);

		m_Dragging = false;
	}
}