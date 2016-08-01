using UnityEngine;
using System.Collections;
using UnityEngine.VR.Tools;
using System;
using System.Collections.Generic;

public class UniversalManipulator : MonoBehaviour, IManipulator
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

	private List<BaseHandle> m_AllHandles = new List<BaseHandle>();

	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }

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

	private void TranslateHandleOnDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		translate(deltaPosition);
	}

	private void RotateHandleOnDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		rotate(deltaRotation);
	}

	private void HandleOnBeginDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		SetAllHandlesActive(false);
		handle.gameObject.SetActive(true);
	}

	private void HandleOnEndDrag(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		SetAllHandlesActive(true);
	}

	private void SetAllHandlesActive(bool active)
	{
		foreach(var handle in m_AllHandles)
			handle.gameObject.SetActive(active);
	}
}
