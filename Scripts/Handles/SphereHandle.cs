using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;
using System;

public class SphereHandle : BaseHandle, IScrollHandler
{
	[SerializeField]
	private Mesh m_InvertedSphereMesh;

	[SerializeField]
	private float kInitialScrollRate = 1.5f;

	[SerializeField]
	private float m_ScrollAcceleration = .8f;
	private float m_ScrollRate;

	private Vector3 m_LastPosition;
	private MeshCollider m_InvertedSphereCollider;
	private float m_CurrentRadius = 0f;

	public override void OnBeginDrag(PointerEventData eventData)
	{
		base.OnBeginDrag(eventData);
		// Get ray origin transform from InputModule and pointerID because the event camera moves between multiple transforms
		m_RayOrigin = ((MultipleRayInputModule)EventSystem.current.currentInputModule).GetRayOrigin(eventData.pointerId);
		m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;

		if (m_InvertedSphereCollider != null)
			DestroyImmediate(m_InvertedSphereCollider.gameObject);

		var invertedSphere = new GameObject();
		invertedSphere.transform.SetParent(transform);
		m_InvertedSphereCollider = invertedSphere.AddComponent<MeshCollider>();
		m_InvertedSphereCollider.sharedMesh = m_InvertedSphereMesh;
		m_InvertedSphereCollider.gameObject.layer = LayerMask.NameToLayer("UI");
		m_CurrentRadius = eventData.pointerCurrentRaycast.distance;
		m_ScrollRate = 0f;
		UpdateScale();
		OnHandleBeginDrag();
	}

	
	public override void OnDrag(PointerEventData eventData)
	{
		base.OnDrag(eventData);
		var worldPosition = m_LastPosition;
		RaycastHit hit;
		if (m_InvertedSphereCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity))
			worldPosition = hit.point;

		var delta = worldPosition - m_LastPosition;
		m_LastPosition = worldPosition;

		m_InvertedSphereCollider.transform.position = m_RayOrigin.position;
		OnHandleDrag(delta);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		base.OnEndDrag(eventData);

		if (m_InvertedSphereCollider != null)
			DestroyImmediate(m_InvertedSphereCollider.gameObject);

		OnHandleEndDrag();
	}

	void OnDestroy()
	{
		if (m_InvertedSphereCollider != null)
			DestroyImmediate(m_InvertedSphereCollider.gameObject);
	}

	private void UpdateScale()
	{
		var inverseParentScale = new Vector3(1 / m_InvertedSphereCollider.transform.parent.lossyScale.x,
			1 / m_InvertedSphereCollider.transform.parent.lossyScale.y, 1 / m_InvertedSphereCollider.transform.parent.lossyScale.z);

		m_InvertedSphereCollider.transform.localScale = inverseParentScale * m_CurrentRadius * 2f;
	}

	public void ChangeRadius(float delta)
	{
		m_CurrentRadius += delta;
		m_CurrentRadius = Mathf.Max(m_CurrentRadius, 0f);
		UpdateScale();
	}

	public void OnScroll(PointerEventData eventData)
	{
		if (m_Dragging)
		{
			if (Mathf.Abs(eventData.scrollDelta.y) > 0.9)
				m_ScrollRate += eventData.scrollDelta.y * m_ScrollAcceleration* Time.unscaledDeltaTime;

			if(Mathf.Abs(eventData.scrollDelta.y) < 0.1)
				m_ScrollRate = kInitialScrollRate;
			ChangeRadius(m_ScrollRate * eventData.scrollDelta.y * Time.unscaledDeltaTime);
		}
	}
}
