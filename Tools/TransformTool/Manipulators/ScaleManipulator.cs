using UnityEngine;
using System.Collections;
using UnityEngine.VR.Tools;
using System;
using System.Collections.Generic;
using UnityEditor.VR;
using UnityEngine.VR;
using UnityEngine.VR.Proxies;

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

	[SerializeField]
	private Transform m_PlaneHandlesParent;

	[SerializeField]
	private BaseHandle m_HandleXY;
	[SerializeField]
	private BaseHandle m_HandleXZ;
	[SerializeField]
	private BaseHandle m_HandleYZ;

	private List<BaseHandle> m_AllHandles = new List<BaseHandle>();
	private bool m_Dragging = false;

	public bool dragging { get { return m_Dragging; } }
	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }
	public Action<Vector3> scale { private get; set; }

	private Vector3 m_WorldPosition;

	void Awake()
	{
		m_AllHandles.Add(m_UniformHandle);
		m_AllHandles.Add(m_HandleX);
		m_AllHandles.Add(m_HandleY);
		m_AllHandles.Add(m_HandleZ);
		m_AllHandles.Add(m_HandleXY);
		m_AllHandles.Add(m_HandleXZ);
		m_AllHandles.Add(m_HandleYZ);
	}
	void OnEnable()
	{
		m_UniformHandle.onHandleDrag += UniformScaleHandleOnDrag;
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

	void Update()
	{
		var viewerPosition = VRView.viewerCamera.transform.position;
		if (!m_Dragging)
		{
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
	
	private void PlaneScaleHandleOnDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		scale(handle.transform.position);
	}

	private void LinearScaleHandleOnDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		scale(handle.transform.position);
	}

	private void UniformScaleHandleOnDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		float delta = deltaPosition.magnitude;
		if (Vector3.Dot(Vector3.one, deltaPosition) < 0)
			delta *= -1;
		scale(Vector3.one * deltaPosition.magnitude);
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
