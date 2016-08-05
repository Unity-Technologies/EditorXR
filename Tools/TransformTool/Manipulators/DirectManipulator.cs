using UnityEngine;
using System.Collections;
using UnityEngine.VR.Tools;
using System;
using System.Collections.Generic;
using UnityEditor.VR;
using UnityEngine.VR;
using UnityEngine.VR.Proxies;

public class DirectManipulator : MonoBehaviour, IManipulator
{
	[SerializeField]
	private BaseHandle m_GrabHandle;

	private List<BaseHandle> m_AllHandles = new List<BaseHandle>();
	private bool m_Dragging = false;
	public bool dragging { get {  return m_Dragging; } }
	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }
	public Action<Vector3> scale { private get; set; }

	void Awake()
	{
		m_AllHandles.Add(m_GrabHandle);
	}
	void OnEnable()
	{
		m_GrabHandle.onHandleDrag += TranslateHandleOnDrag;
		foreach (var handle in m_AllHandles)
		{
			handle.onHandleBeginDrag += HandleOnBeginDrag;
			handle.onHandleEndDrag += HandleOnEndDrag;
		}
	}

	void OnDisable()
	{
		m_GrabHandle.onHandleDrag -= TranslateHandleOnDrag;

		foreach (var handle in m_AllHandles)
		{
			handle.onHandleBeginDrag -= HandleOnBeginDrag;
			handle.onHandleEndDrag -= HandleOnEndDrag;
		}
	}

	private void TranslateHandleOnDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		translate(deltaPosition);
	}

	private void HandleOnBeginDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		SetAllHandlesActive(false);
		handle.gameObject.SetActive(true);
		m_Dragging = true;
	}

	private void HandleOnEndDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
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
