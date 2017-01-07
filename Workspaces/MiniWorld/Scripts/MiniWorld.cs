using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Utilities;

public class MiniWorld : MonoBehaviour, IMiniWorld
{
	public LayerMask rendererCullingMask
	{
		get { return m_RendererCullingMask; }
		set
		{
			m_RendererCullingMask = value;
			if (m_MiniWorldRenderer)
				m_MiniWorldRenderer.cullingMask = m_RendererCullingMask;
		}
	}

	[SerializeField]
	private LayerMask m_RendererCullingMask = -1;

	private Vector3 m_LocalBoundsSize = Vector3.one;

	private MiniWorldRenderer m_MiniWorldRenderer;

	public Transform miniWorldTransform { get { return transform; } }
	
	/// <summary>
	/// ReferenceTransform defines world space within the MiniWorld. When scaled up, a larger area is represented,
	/// thus the objects in the MiniWorld get smaller.
	/// </summary>
	public Transform referenceTransform { get { return m_ReferenceTransform; } set { m_ReferenceTransform = value; } }
	[SerializeField]
	Transform m_ReferenceTransform;

	public Matrix4x4 miniToReferenceMatrix { get { return transform.localToWorldMatrix * referenceTransform.worldToLocalMatrix; } }

	public Func<Camera, Matrix4x4> getWorldToCameraMatrix { get { return m_MiniWorldRenderer.GetWorldToCameraMatrix; } }

	public Bounds referenceBounds
	{
		get { return new Bounds(referenceTransform.position, Vector3.Scale(referenceTransform.localScale, m_LocalBoundsSize)); }
		set
		{
			referenceTransform.position = value.center;
			m_LocalBoundsSize = Vector3.Scale(referenceTransform.localScale.Inverse(), value.size);
		}
	}

	public Bounds localBounds { get { return new Bounds(Vector3.zero, m_LocalBoundsSize); } set { m_LocalBoundsSize = value.size; } }

	public bool Contains(Vector3 position)
	{
		return localBounds.Contains(transform.InverseTransformPoint(position));
	}

	public List<Renderer> ignoreList { set { m_MiniWorldRenderer.ignoreList = value; } }

	private void OnEnable()
	{
		if (!referenceTransform)
			referenceTransform = new GameObject("MiniWorldReference") { hideFlags = HideFlags.DontSave }.transform;

		m_MiniWorldRenderer = U.Object.AddComponent<MiniWorldRenderer>(U.Camera.GetMainCamera().gameObject);
		m_MiniWorldRenderer.miniWorld = this;
		m_MiniWorldRenderer.cullingMask = m_RendererCullingMask;

		Transform pivot = U.Camera.GetViewerPivot();
		referenceTransform.position = pivot.transform.position;
	}

	private void OnDisable()
	{
		if ((referenceTransform.hideFlags & HideFlags.DontSave) != 0)
			U.Object.Destroy(referenceTransform.gameObject);

		if (m_MiniWorldRenderer)
			U.Object.Destroy(m_MiniWorldRenderer);
	}
}