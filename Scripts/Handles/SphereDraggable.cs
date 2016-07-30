using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;

public class SphereDraggable : BaseDraggable
{
	[SerializeField]
	private Mesh m_InvertedSphereMesh;

	private Vector3 m_LastPosition;
	protected MeshCollider m_InvertedSphereCollider;

	public override void OnBeginDrag(PointerEventData eventData)
	{
		base.OnBeginDrag(eventData);
		// Get ray origin transform from InputModule and pointerID because the event camera moves between multiple transforms
		m_RayOrigin = ((MultipleRayInputModule)EventSystem.current.currentInputModule).GetRayOrigin(eventData.pointerId);
		m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;

		if (m_InvertedSphereCollider != null)
			DestroyImmediate(m_InvertedSphereCollider.gameObject);

		var invertedSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		invertedSphere.transform.SetParent(eventData.pressEventCamera.transform.parent);

		DestroyImmediate(invertedSphere.GetComponent<SphereCollider>());
		invertedSphere.GetComponent<MeshFilter>().sharedMesh = m_InvertedSphereMesh;
		m_InvertedSphereCollider = invertedSphere.AddComponent<MeshCollider>();
		m_InvertedSphereCollider.sharedMesh = m_InvertedSphereMesh;

		var inverseParentScale = new Vector3(1 / m_InvertedSphereCollider.transform.parent.lossyScale.x,
			1 / m_InvertedSphereCollider.transform.parent.lossyScale.y, 1 / m_InvertedSphereCollider.transform.parent.lossyScale.z);
		m_InvertedSphereCollider.transform.localScale = inverseParentScale * eventData.pointerCurrentRaycast.distance * 2f;

		//m_InvertedSphereCollider.GetComponent<Renderer>().enabled = false;
		m_InvertedSphereCollider.GetComponent<Renderer>().sharedMaterial = m_DebugMaterial;
		m_Collider.enabled = false;
		m_InvertedSphereCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		RaiseBeginDrag();
	}

	
	public override void OnDrag(PointerEventData eventData)
	{
		Vector3 worldPosition = m_LastPosition;
		RaycastHit hit;
		if (m_InvertedSphereCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity)) //TODO cache collider
			worldPosition = hit.point;

		var delta = worldPosition - m_LastPosition;
		m_LastPosition = worldPosition;

		m_InvertedSphereCollider.transform.position = m_RayOrigin.position;

		RaiseDrag(delta);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		base.OnEndDrag(eventData);

		if (m_InvertedSphereCollider != null)
			DestroyImmediate(m_InvertedSphereCollider.gameObject);

		RaiseEndDrag();
	}

	void OnDestroy()
	{
		if (m_InvertedSphereCollider != null)
			DestroyImmediate(m_InvertedSphereCollider.gameObject);
	}
}
