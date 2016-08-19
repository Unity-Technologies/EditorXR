using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour, IInstantiateUI, IHighlight
{
	public static readonly Vector3		kDefaultBounds = new Vector3(0.6f, 0.4f, 0.4f);
	public static readonly Vector3		kDefaultOffset = new Vector3(0, -0.15f, 1f);
	public static readonly Quaternion	kDefaultTilt = Quaternion.AngleAxis(-20, Vector3.right);

	public const float kHandleMargin = 0.25f;	// Amount of space (in World units) between handle and content bounds in X and Z
	public const float kContentHeight = 0.075f;	// Amount of height (in World units) between tray and content bounds
	//Extra space for tray model
	public const float kExtraHeight = 0.15f;
	public const float kExtraWidth = 0.15f;
	public const float kExtraDepth = 0.2f;

	/// <summary>
	/// Bounding box for workspace content (ignores value.center) 
	/// </summary>
	public Bounds contentBounds
	{
		get { return m_ContentBounds; }
		set
		{
			if (!value.Equals(contentBounds))
			{
				Vector3 size = value.size;
				if (size.x < kDefaultBounds.x) //Use defaultBounds until we need separate values
					size.x = kDefaultBounds.x;
				if (size.y < kDefaultBounds.y)
					size.y = kDefaultBounds.y;
				if (size.z < kDefaultBounds.z)
					size.z = kDefaultBounds.z;
				value.size = size;
				m_ContentBounds.size = size; //Only set size, ignore center.
				m_WorkspaceSceneObjects.SetBounds(contentBounds);
				OnBoundsChanged();
			}
		}
	}

	[SerializeField]
	private Bounds m_ContentBounds;

	[SerializeField]
	private float m_VacuumTime = 0.75f;

	protected WorkspaceSceneObjects m_WorkspaceSceneObjects;

	[SerializeField]
	private GameObject m_BasePrefab;

	private Transform m_LastParent;
	private Vector3 m_DragStart;
	private Vector3 m_PositionStart;
	private Vector3 m_BoundSizeStart;
	private bool m_Dragging;

	/// <summary>
	/// Bounding box for entire workspace, including UI handles
	/// </summary>
	public Bounds outerBounds
	{
		get
		{
			return new Bounds(contentBounds.center + Vector3.down * kContentHeight * 0.5f,
				new Vector3(
					contentBounds.size.x + kExtraWidth + kHandleMargin,
					contentBounds.size.y + kExtraHeight + kContentHeight,
					contentBounds.size.z + kExtraDepth + kHandleMargin
					));
		}
	}

	public Func<GameObject, GameObject> instantiateUI { private get; set; }

	public Action<GameObject, bool> setHighlight { get; set; }

	public virtual void Setup()
	{
		GameObject baseObject = instantiateUI(m_BasePrefab);
		baseObject.transform.SetParent(transform, false);
		
		m_WorkspaceSceneObjects = baseObject.GetComponent<WorkspaceSceneObjects>();
		m_WorkspaceSceneObjects.OnCloseClick = Close;
		m_WorkspaceSceneObjects.sceneContainer.transform.localPosition = Vector3.up * kContentHeight;  

		//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
		m_ContentBounds = new Bounds(Vector3.up * kDefaultBounds.y * 0.5f, kDefaultBounds);
		m_WorkspaceSceneObjects.SetBounds(contentBounds);

		m_WorkspaceSceneObjects.directManipulator.target = transform;

		m_WorkspaceSceneObjects.vacuumHandle.onDoubleClick += OnDoubleClick;

		var handles = new BaseHandle[]
		{
			m_WorkspaceSceneObjects.leftHandle,
			m_WorkspaceSceneObjects.frontHandle,
			m_WorkspaceSceneObjects.backHandle,
			m_WorkspaceSceneObjects.rightHandle
		};

		foreach (var handle in handles)
		{
			handle.onHandleBeginDrag += OnHandleBeginDrag;
			handle.onHandleDrag += OnHandleDrag;
			handle.onHandleEndDrag += OnHandleEndDrag;
			
			handle.onHoverEnter += OnHandleHoverEnter;
			handle.onHoverExit += OnHandleHoverExit;
		}
	}

	protected abstract void OnBoundsChanged();

	public virtual void OnHandleBeginDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		m_PositionStart = transform.position;
		m_DragStart = eventData.rayOrigin.position;
		m_BoundSizeStart = contentBounds.size;
		m_Dragging = true;
	}

	public virtual void OnHandleDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		if (m_Dragging)
		{
			Vector3 dragVector = eventData.rayOrigin.position - m_DragStart;
			Bounds bounds = contentBounds;
			Vector3 positionOffset = Vector3.zero;
			if (handle.Equals(m_WorkspaceSceneObjects.leftHandle))
			{
				bounds.size = m_BoundSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right);
				positionOffset = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;
			}
			if (handle.Equals(m_WorkspaceSceneObjects.frontHandle))
			{
				bounds.size = m_BoundSizeStart + Vector3.back * Vector3.Dot(dragVector, transform.forward);
				positionOffset = transform.forward * Vector3.Dot(dragVector, transform.forward) * 0.5f;
			}
			if (handle.Equals(m_WorkspaceSceneObjects.rightHandle))
			{
				bounds.size = m_BoundSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right);
				positionOffset = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;
			}
			if (handle.Equals(m_WorkspaceSceneObjects.backHandle))
			{
				bounds.size = m_BoundSizeStart + Vector3.forward * Vector3.Dot(dragVector, transform.forward);
				positionOffset = transform.forward * Vector3.Dot(dragVector, transform.forward) * 0.5f;
			}
			contentBounds = bounds;
			if(contentBounds.size == bounds.size) //Don't reposition if we hit minimum bounds
				transform.position = m_PositionStart + positionOffset;
		}
	}

	public virtual void OnHandleEndDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		m_Dragging = false;
	}

	public virtual void OnHandleHoverEnter(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		setHighlight(handle.gameObject, true);
	}

	public virtual void OnHandleHoverExit(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		setHighlight(handle.gameObject, false);
	}

	private void OnDoubleClick(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		StartCoroutine(VacuumToViewer());
	}

	private IEnumerator VacuumToViewer()
	{
		float startTime = Time.realtimeSinceStartup;
		Vector3 startPosition = transform.position;
		Quaternion startRotation = transform.rotation;

		Transform camera = U.Camera.GetMainCamera().transform;
		Vector3 destPosition = camera.position + camera.rotation * kDefaultOffset;

		Vector3 cameraYawVector = camera.forward;
		cameraYawVector.y = 0;
		Quaternion destRotation = Quaternion.LookRotation(cameraYawVector, Vector3.up) * kDefaultTilt;

		while (Time.realtimeSinceStartup < startTime + m_VacuumTime)
		{
			transform.position = Vector3.Lerp(startPosition, destPosition, (Time.realtimeSinceStartup - startTime) / m_VacuumTime);
			transform.rotation = Quaternion.Lerp(startRotation, destRotation, (Time.realtimeSinceStartup - startTime) / m_VacuumTime);
			yield return null;
		}
		transform.position = destPosition;
		transform.rotation = destRotation;
	}

	public virtual void Close()
	{
		U.Object.Destroy(gameObject);
	}
}