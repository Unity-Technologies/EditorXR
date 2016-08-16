using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class SphereHandle : BaseHandle, IRayDragHandler, IScrollHandler
	{
		[SerializeField] private Mesh m_InvertedSphereMesh;

		private const float kInitialScrollRate = 2f;
		private const float kScrollAcceleration = 14f;
		private float m_ScrollRate;

		private Vector3 m_LastPosition;
		private MeshCollider m_InvertedSphereCollider;
		private float m_CurrentRadius = 0f;

		public override void OnBeginDrag(RayEventData eventData)
		{
			base.OnBeginDrag(eventData);
			m_RayOrigin = eventData.rayOrigin;
			m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;

			if (m_InvertedSphereCollider != null)
				DestroyImmediate(m_InvertedSphereCollider.gameObject);

			var invertedSphere = new GameObject {name = "InvertedSphereCollider"};
			invertedSphere.transform.SetParent(transform);
			m_InvertedSphereCollider = invertedSphere.AddComponent<MeshCollider>();
			m_InvertedSphereCollider.sharedMesh = m_InvertedSphereMesh;
			m_InvertedSphereCollider.gameObject.layer = LayerMask.NameToLayer("UI");
			m_CurrentRadius = eventData.pointerCurrentRaycast.distance;
			m_ScrollRate = kInitialScrollRate;
			UpdateScale();
			OnHandleBeginDrag();
		}

		public void OnDrag(RayEventData eventData)
		{
			var worldPosition = m_LastPosition;
			m_InvertedSphereCollider.transform.position = m_RayOrigin.position;

			RaycastHit hit;
			if (m_InvertedSphereCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity))
				worldPosition = hit.point;

			var delta = worldPosition - m_LastPosition;
			m_LastPosition = worldPosition;

			OnHandleDrag(new HandleDragEventData(delta));
		}

		public override void OnEndDrag(RayEventData eventData)
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
			// Update the sphere's local scale to a fixed radius by inverting the parent's lossy scale
			var inverseParentScale = new Vector3(1/m_InvertedSphereCollider.transform.parent.lossyScale.x,
				1/m_InvertedSphereCollider.transform.parent.lossyScale.y, 1/m_InvertedSphereCollider.transform.parent.lossyScale.z);

			m_InvertedSphereCollider.transform.localScale = inverseParentScale*m_CurrentRadius*2f;
		}

		public void ChangeRadius(float delta)
		{
			m_CurrentRadius += delta;
			m_CurrentRadius = Mathf.Max(m_CurrentRadius, 0f);
			UpdateScale();
		}

		public void OnScroll(PointerEventData eventData)
		{
			if (!m_Dragging)
				return;

			// Scolling changes the radius of the sphere while dragging, and accelerates
			if (Mathf.Abs(eventData.scrollDelta.y) > 0.5f)
				m_ScrollRate += Mathf.Abs(eventData.scrollDelta.y)*kScrollAcceleration*Time.unscaledDeltaTime;
			else
				m_ScrollRate = kInitialScrollRate;

			ChangeRadius(m_ScrollRate*eventData.scrollDelta.y*Time.unscaledDeltaTime);
		}
	}
}