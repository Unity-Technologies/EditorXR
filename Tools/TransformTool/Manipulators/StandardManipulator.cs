using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Tools;
using UnityEditor.VR;
using UnityEngine.VR.Handles;

public class StandardManipulator : MonoBehaviour, IManipulator
{
	[SerializeField]
	private BaseHandle m_GrabHandle;

	[SerializeField]
	private BaseHandle m_TranslateHandleX;
	[SerializeField]
	private BaseHandle m_TranslateHandleY;
	[SerializeField]
	private BaseHandle m_TranslateHandleZ;

	[SerializeField]
	private Transform m_PlaneHandlesParent;
	[SerializeField]
	private BaseHandle m_TranslateHandleXY;
	[SerializeField]
	private BaseHandle m_TranslateHandleXZ;
	[SerializeField]
	private BaseHandle m_TranslateHandleYZ;

	[SerializeField]
	private BaseHandle m_RotateHandleX;
	[SerializeField]
	private BaseHandle m_RotateHandleY;
	[SerializeField]
	private BaseHandle m_RotateHandleZ;

	private readonly List<BaseHandle> m_AllHandles = new List<BaseHandle>();
	private bool m_Dragging = false;
	public bool dragging { get {  return m_Dragging; } }
	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }
	public Action<Vector3> scale { private get; set; }

	void Awake()
	{
		m_AllHandles.Add(m_GrabHandle);
		m_AllHandles.Add(m_TranslateHandleX);
		m_AllHandles.Add(m_TranslateHandleY);
		m_AllHandles.Add(m_TranslateHandleZ);
		m_AllHandles.Add(m_TranslateHandleXY);
		m_AllHandles.Add(m_TranslateHandleXZ);
		m_AllHandles.Add(m_TranslateHandleYZ);
		m_AllHandles.Add(m_RotateHandleX);
		m_AllHandles.Add(m_RotateHandleY);
		m_AllHandles.Add(m_RotateHandleZ);
	}
	void OnEnable()
	{
		m_GrabHandle.onHandleDrag += TranslateHandleOnDrag;

		m_TranslateHandleX.onHandleDrag += TranslateHandleOnDrag;
		m_TranslateHandleY.onHandleDrag += TranslateHandleOnDrag;
		m_TranslateHandleZ.onHandleDrag += TranslateHandleOnDrag;

		m_TranslateHandleXY.onHandleDrag += TranslateHandleOnDrag;
		m_TranslateHandleXZ.onHandleDrag += TranslateHandleOnDrag;
		m_TranslateHandleYZ.onHandleDrag += TranslateHandleOnDrag;

		m_RotateHandleX.onHandleDrag += RotateHandleOnDrag;
		m_RotateHandleY.onHandleDrag += RotateHandleOnDrag;
		m_RotateHandleZ.onHandleDrag += RotateHandleOnDrag;

		foreach (var handle in m_AllHandles)
		{
			handle.onHandleBeginDrag += HandleOnBeginDrag;
			handle.onHandleEndDrag += HandleOnEndDrag;
		}
	}

	void OnDisable()
	{
		m_GrabHandle.onHandleDrag -= TranslateHandleOnDrag;

		m_TranslateHandleX.onHandleDrag -= TranslateHandleOnDrag;
		m_TranslateHandleY.onHandleDrag -= TranslateHandleOnDrag;
		m_TranslateHandleZ.onHandleDrag -= TranslateHandleOnDrag;

		m_TranslateHandleXY.onHandleDrag -= TranslateHandleOnDrag;
		m_TranslateHandleXZ.onHandleDrag -= TranslateHandleOnDrag;
		m_TranslateHandleYZ.onHandleDrag -= TranslateHandleOnDrag;

		m_RotateHandleX.onHandleDrag -= RotateHandleOnDrag;
		m_RotateHandleY.onHandleDrag -= RotateHandleOnDrag;
		m_RotateHandleZ.onHandleDrag -= RotateHandleOnDrag;

		foreach (var handle in m_AllHandles)
		{
			handle.onHandleBeginDrag -= HandleOnBeginDrag;
			handle.onHandleEndDrag -= HandleOnEndDrag;
		}
	}

	void Update()
	{
		if (!m_Dragging)
		{
			var viewerPosition = VRView.viewerCamera.transform.position;
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

	private void TranslateHandleOnDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		translate(eventData.deltaPosition);
	}

	private void RotateHandleOnDrag(BaseHandle handle, HandleDragEventData eventData)
	{
		rotate(eventData.deltaRotation);
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
