using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;

public class DirectManipulator : MonoBehaviour, IManipulator
{
	public Transform target { set { m_Target = value; } }
	[SerializeField]
	private Transform m_Target;

	[SerializeField]
	private List<BaseHandle> m_AllHandles = new List<BaseHandle>();

	public bool dragging { get {  return m_Dragging; } }
	private bool m_Dragging;

	private Vector3 m_PositionOffset;
	private Quaternion m_RotationOffset;
	
	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }
	public Action<Vector3> scale { private get; set; }

	void OnEnable()
	{
		foreach (var h in m_AllHandles)
		{
			h.handleDrag += OnHandleDrag;
			h.handleDragging += OnHandleDragging;
			h.handleDragged += OnHandleDragged;
		}
	}

	void OnDisable()
	{
		foreach (var h in m_AllHandles)
		{
			h.handleDrag -= OnHandleDrag;
			h.handleDragging -= OnHandleDragging;
			h.handleDragged -= OnHandleDragged;
		}
	}

	private void OnHandleDragging(BaseHandle handle, HandleEventData eventData)
	{
		foreach (var h in m_AllHandles)
			h.gameObject.SetActive(h == handle);
		m_Dragging = true;

		Transform target = m_Target ?? transform;

		var rayOrigin = eventData.rayOrigin;
		var inverseRotation = Quaternion.Inverse(rayOrigin.rotation);
		m_PositionOffset = inverseRotation * (target.transform.position - rayOrigin.position);
		m_RotationOffset = inverseRotation * target.transform.rotation;
	}

	private void OnHandleDrag(BaseHandle handle, HandleEventData eventData)
	{
		Transform target = m_Target ?? transform;

		var rayOrigin = eventData.rayOrigin;
		translate(rayOrigin.position + rayOrigin.rotation * m_PositionOffset - target.position);
		rotate(Quaternion.Inverse(target.rotation) * rayOrigin.rotation * m_RotationOffset);
	}

	private void OnHandleDragged(BaseHandle handle, HandleEventData eventData)
	{
		foreach (var h in m_AllHandles)
			h.gameObject.SetActive(true);

		m_Dragging = false;
	}
}
