using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour, IInstantiateUI, IHighlight
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

	private Transform m_LastParent;
	private Vector3 dragStart;
	private Vector3 positionStart;
	private Vector3 boundSizeStart;
	private bool m_Dragging;

	public Action<GameObject, bool> setHighlight { get; set; }

	public virtual void Setup()
	{
		GameObject baseObject = instantiateUI(m_BasePrefab);
		baseObject.transform.SetParent(transform);
		m_WorkspacePrefab = baseObject.GetComponent<WorkspacePrefab>();
		m_WorkspacePrefab.OnCloseClick = Close;
		m_WorkspacePrefab.sceneContainer.transform.localPosition = Vector3.up * kContentHeight;
		baseObject.transform.localPosition = Vector3.zero;
		baseObject.transform.localRotation = Quaternion.identity;  
		//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
		m_ContentBounds = new Bounds(Vector3.up * kDefaultBounds.y * 0.5f, kDefaultBounds);
		m_WorkspacePrefab.SetBounds(contentBounds);

		m_WorkspacePrefab.translateHandle.onHandleBeginDrag += OnTransformDragStart;
		m_WorkspacePrefab.translateHandle.onHandleEndDrag += OnTransformDragEnd;

		var handles = new List<BaseHandle>(4);
		handles.Add(m_WorkspacePrefab.leftHandle);
		handles.Add(m_WorkspacePrefab.frontHandle);
		handles.Add(m_WorkspacePrefab.backHandle);
		handles.Add(m_WorkspacePrefab.rightHandle);

		foreach (var handle in handles)
		{
			handle.onHandleBeginDrag += OnHandleDragStart;
			handle.onHandleDrag += OnHandleDrag;
			handle.onHandleEndDrag += OnHandleDragEnd;
			
			handle.onHoverEnter += OnHandleHoverEnter;
			handle.onHoverExit += OnHandleHoverExit;
		}
	}

	protected abstract void OnBoundsChanged();

	public virtual void OnTransformDragStart(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		m_LastParent = transform.parent;
		transform.parent = eventData.rayOrigin;
	}

	public virtual void OnTransformDragEnd(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		transform.parent = m_LastParent;
	}

	public virtual void OnHandleDragStart(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		positionStart = transform.position;
		dragStart = eventData.rayOrigin.position;
		boundSizeStart = contentBounds.size;
		m_Dragging = true;
	}

	public virtual void OnHandleDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		if (m_Dragging)
		{
			Vector3 dragVector = eventData.rayOrigin.position - dragStart;
			Bounds tmpBounds = contentBounds;
			Vector3 positionOffset = Vector3.zero;
			if (handle.Equals(m_WorkspacePrefab.leftHandle))
			{
				tmpBounds.size = boundSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right);
				positionOffset = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;
			}
			if (handle.Equals(m_WorkspacePrefab.frontHandle))
			{
				tmpBounds.size = boundSizeStart + Vector3.back * Vector3.Dot(dragVector, transform.forward);
				positionOffset = transform.forward * Vector3.Dot(dragVector, transform.forward) * 0.5f;
			}
			if (handle.Equals(m_WorkspacePrefab.rightHandle))
			{
				tmpBounds.size = boundSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right);
				positionOffset = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;
			}
			if (handle.Equals(m_WorkspacePrefab.backHandle))
			{
				tmpBounds.size = boundSizeStart + Vector3.forward * Vector3.Dot(dragVector, transform.forward);
				positionOffset = transform.forward * Vector3.Dot(dragVector, transform.forward) * 0.5f;
			}
			contentBounds = tmpBounds;
			transform.position = positionStart + positionOffset;
		}
	}

	public virtual void OnHandleDragEnd(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		m_Dragging = false;
	}

	public virtual void OnHandleHoverEnter(BaseHandle handle)
	{
		setHighlight(handle.gameObject, true);
	}

	public virtual void OnHandleHoverExit(BaseHandle handle)
	{
		setHighlight(handle.gameObject, false);
	}

	public virtual void Close()
	{
		U.Object.Destroy(gameObject);
	}
}