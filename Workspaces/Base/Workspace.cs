using System;
using UnityEngine;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour, IInstantiateUI
{
	public enum Direction { LEFT, FRONT, RIGHT, BACK}
	public static readonly Vector3 kDefaultBounds = new Vector3(0.6f, 0.4f, 0.4f);

	/// <summary>
	/// Amount of space (in World units) between handle and content bounds in X and Z
	/// </summary>
	public const float kHandleMargin = 0.25f;

	/// <summary>
	/// Amount of height (in World units) between handle and content bounds
	/// </summary>
	public const float kContentHeight = 0.075f;

	/// <summary>
	/// Bounding box for workspace content. 
	/// </summary>
	public Bounds contentBounds
	{
		get { return m_ContentBounds; }
		set
		{
			if (!value.Equals(contentBounds))
			{
				value.center = contentBounds.center;
				m_ContentBounds = value;
				m_WorkspacePrefab.SetBounds(contentBounds);
				OnBoundsChanged();
			}
		}
	}
	[SerializeField]
	private Bounds m_ContentBounds;

	/// <summary>
	/// Bounding box for entire workspace, including UI handles
	/// </summary>
	public Bounds outerBounds
	{
		get
		{
			return new Bounds(contentBounds.center + Vector3.down * kContentHeight * 0.5f,
				new Vector3(
					contentBounds.size.x + kHandleMargin,
					contentBounds.size.y + kContentHeight,
					contentBounds.size.z + kHandleMargin
					));
		}
	}

	public Func<GameObject, GameObject> instantiateUI { private get; set; }
	protected WorkspacePrefab m_WorkspacePrefab;

	[SerializeField]
	private GameObject m_BasePrefab;

	private Vector3 dragStart;
	private Vector3 positionStart;
	private Vector3 boundSizeStart;

	public virtual void Setup()
	{
		GameObject baseObject = instantiateUI(m_BasePrefab);
		baseObject.transform.SetParent(transform);
		m_WorkspacePrefab = baseObject.GetComponent<WorkspacePrefab>();
		m_WorkspacePrefab.OnHandleDragStart = OnHandleDragStart;
		m_WorkspacePrefab.OnHandleDrag = OnHandleDrag;
		m_WorkspacePrefab.OnCloseClick = Close;
		m_WorkspacePrefab.sceneContainer.transform.localPosition = Vector3.up * kContentHeight;
		baseObject.transform.localPosition = Vector3.zero;
		baseObject.transform.localRotation = Quaternion.identity;  
		//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
		m_ContentBounds = new Bounds(Vector3.up * kDefaultBounds.y * 0.5f, kDefaultBounds);
		m_WorkspacePrefab.SetBounds(contentBounds);

		//Set grab handle selection target to this transform
		m_WorkspacePrefab.grabHandle.selectionTarget = gameObject;
	}

	protected abstract void OnBoundsChanged();

	public virtual void OnHandleDragStart(Transform handle, Transform rayOrigin)
	{
		positionStart = transform.position;
		dragStart = rayOrigin.position;
		boundSizeStart = contentBounds.size;
	}

	public virtual void OnHandleDrag(Transform rayOrigin, Direction direction)
	{
		Vector3 dragVector = rayOrigin.position - dragStart;
		Debug.DrawLine(transform.position, transform.position + transform.right * 10);
		Bounds tmpBounds = contentBounds;
		Vector3 positionOffset = Vector3.zero;
		switch (direction)
		{
			case Direction.LEFT:
				tmpBounds.size = boundSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right);
				positionOffset = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;
				break;
			case Direction.FRONT:
				tmpBounds.size = boundSizeStart + Vector3.back * Vector3.Dot(dragVector, transform.forward);
				positionOffset = transform.forward * Vector3.Dot(dragVector, transform.forward) * 0.5f;
				break;
			case Direction.RIGHT:
				tmpBounds.size = boundSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right);
				positionOffset = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;
				break;
			case Direction.BACK:
				tmpBounds.size = boundSizeStart + Vector3.forward * Vector3.Dot(dragVector, transform.forward);
				positionOffset = transform.forward * Vector3.Dot(dragVector, transform.forward) * 0.5f;
				break;
		}
		contentBounds = tmpBounds;
		transform.position = positionStart + positionOffset;
	}

	public virtual void Close()
	{
		U.Object.Destroy(gameObject);
	}
}