using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;

public class DirectHandle : BaseHandle
{
	[SerializeField]
	private Mesh m_InvertedSphereMesh;

	private Vector3 m_LastPosition;
	private MeshCollider m_InvertedSphereCollider;

	public override void OnBeginDrag(PointerEventData eventData)
	{
		if(eventData.pointerCurrentRaycast.distance > .05f)
			return;
		base.OnBeginDrag(eventData);
		// Get ray origin transform from InputModule and pointerID because the event camera moves between multiple transforms
		m_RayOrigin = ((MultipleRayInputModule)EventSystem.current.currentInputModule).GetRayOrigin(eventData.pointerId);
		m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;

		if (m_InvertedSphereCollider != null)
			DestroyImmediate(m_InvertedSphereCollider.gameObject);

		var invertedSphere = new GameObject();
		invertedSphere.transform.SetParent(eventData.pressEventCamera.transform.parent);
		m_InvertedSphereCollider = invertedSphere.AddComponent<MeshCollider>();
		m_InvertedSphereCollider.sharedMesh = m_InvertedSphereMesh;
		m_InvertedSphereCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		var inverseParentScale = new Vector3(1 / m_InvertedSphereCollider.transform.parent.lossyScale.x,
			1 / m_InvertedSphereCollider.transform.parent.lossyScale.y, 1 / m_InvertedSphereCollider.transform.parent.lossyScale.z);
		m_InvertedSphereCollider.transform.localScale = inverseParentScale * eventData.pointerCurrentRaycast.distance * 2f;

		OnHandleBeginDrag();
	}

	
	public override void OnDrag(PointerEventData eventData)
	{
		if(eventData.pointerCurrentRaycast.distance > .05f)
			return;
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
		if(eventData.pointerCurrentRaycast.distance > .05f)
			return;
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
}
