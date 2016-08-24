using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour, IInstantiateUI, IHighlight
{
	public static readonly Vector3		kDefaultBounds = new Vector3(0.7f, 0.4f, 0.4f);
	public static readonly Vector3		kDefaultOffset = new Vector3(0, -0.15f, 1f);
	public static readonly Quaternion	kDefaultTilt = Quaternion.AngleAxis(-20, Vector3.right);

	public const float kHandleMargin = -0.15f;	// Compensate for base size from frame model

	public Action<Workspace> OnClose { private get; set; }

	protected WorkspaceUI m_WorkspaceUI;

	//Extra space for frame model
	private const float kExtraHeight = 0.15f;

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
				BoundsChanged();
				OnBoundsChanged();
			}
		}
	}
	[SerializeField]
	private Bounds m_ContentBounds;

	[SerializeField]
	private float m_VacuumTime = 0.75f;

	//This must be set on the script object which extends Workspace
	[SerializeField]
	private GameObject m_BasePrefab;

	private Vector3 m_DragStart;
	private Vector3 m_PositionStart;
	private Vector3 m_BoundSizeStart;
	private bool m_Dragging;
	private bool m_DragLocked;

	public bool vacuuming { get; set; }

	/// <summary>
	/// Bounding box for entire workspace, including UI handles
	/// </summary>
	public Bounds outerBounds
	{
		get
		{
			return new Bounds(contentBounds.center + Vector3.down * kExtraHeight * 0.5f,
				new Vector3(
					contentBounds.size.x,
					contentBounds.size.y + kExtraHeight,
					contentBounds.size.z
					));
		}
	}

	public Func<GameObject, GameObject> instantiateUI { private get; set; }

	public Action<GameObject, bool> setHighlight { get; set; }

	public bool vacuumEnabled
	{
		set
		{
			m_WorkspaceUI.vacuumHandle.gameObject.SetActive(value);
		}
	}

	public virtual void Setup()
	{
		GameObject baseObject = instantiateUI(m_BasePrefab);
		baseObject.transform.SetParent(transform, false);
		
		m_WorkspaceUI = baseObject.GetComponent<WorkspaceUI>();
		m_WorkspaceUI.OnCloseClick = Close;
		m_WorkspaceUI.OnLockClick = Lock;
		m_WorkspaceUI.sceneContainer.transform.localPosition = Vector3.zero;

		//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
		m_ContentBounds = new Bounds(Vector3.up * kDefaultBounds.y * 0.5f, kDefaultBounds);
		BoundsChanged();

		//Set up DirectManipulaotr
		var directManipulator = m_WorkspaceUI.directManipulator;
		directManipulator.target = transform;
		directManipulator.translate = Translate;
		directManipulator.rotate = Rotate;

		m_WorkspaceUI.vacuumHandle.onDoubleClick += OnDoubleClick;
		m_WorkspaceUI.vacuumHandle.onHoverEnter += OnHandleHoverEnter;
		m_WorkspaceUI.vacuumHandle.onHoverExit += OnHandleHoverExit;

		var handles = new BaseHandle[]
		{
			m_WorkspaceUI.leftHandle,
			m_WorkspaceUI.frontHandle,
			m_WorkspaceUI.backHandle,
			m_WorkspaceUI.rightHandle
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

	public virtual void OnHandleBeginDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		m_PositionStart = transform.position;
		m_DragStart = eventData.rayOrigin.position;
		m_BoundSizeStart = contentBounds.size;
		m_Dragging = true;
	}

	public virtual void OnHandleDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		if (m_Dragging && !m_DragLocked)
		{
			Vector3 dragVector = eventData.rayOrigin.position - m_DragStart;
			Bounds bounds = contentBounds;
			Vector3 positionOffset = Vector3.zero;
			if (handle.Equals(m_WorkspaceUI.leftHandle))
			{
				bounds.size = m_BoundSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right);
				positionOffset = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;
			}
			if (handle.Equals(m_WorkspaceUI.frontHandle))
			{
				bounds.size = m_BoundSizeStart + Vector3.back * Vector3.Dot(dragVector, transform.forward);
				positionOffset = transform.forward * Vector3.Dot(dragVector, transform.forward) * 0.5f;
			}
			if (handle.Equals(m_WorkspaceUI.rightHandle))
			{
				bounds.size = m_BoundSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right);
				positionOffset = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;
			}
			if (handle.Equals(m_WorkspaceUI.backHandle))
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
		if(handle == m_WorkspaceUI.vacuumHandle || !m_DragLocked)
			setHighlight(handle.gameObject, true);
	}

	public virtual void OnHandleHoverExit(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		if (handle == m_WorkspaceUI.vacuumHandle || !m_DragLocked)
			setHighlight(handle.gameObject, false);
	}

	private void OnDoubleClick(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		StartCoroutine(VacuumToViewer());
	}

	private void Translate(Vector3 deltaPosition)
	{
		if (m_DragLocked) return;
		transform.position += deltaPosition;
	}

	private void Rotate(Quaternion deltaRotation)
	{
		if (m_DragLocked) return;
		transform.rotation *= deltaRotation;
	}

	private IEnumerator VacuumToViewer()
	{
		float startTime = Time.realtimeSinceStartup;
		Vector3 startPosition = transform.position;
		Quaternion startRotation = transform.rotation;

		Transform camera = U.Camera.GetMainCamera().transform;
		var cameraYawVector = camera.forward;
		cameraYawVector.y = 0;
		var cameraYaw = Quaternion.LookRotation(cameraYawVector, Vector3.up);

		Vector3 destPosition = camera.position + cameraYaw * kDefaultOffset;

		Quaternion destRotation = cameraYaw * kDefaultTilt;

		vacuuming = true;
		var vacuumObject = m_WorkspaceUI.vacuumHandle.gameObject;
		setHighlight(vacuumObject, false);
		vacuumObject.SetActive(false);
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
		OnClose(this);
		U.Object.Destroy(gameObject);
	}

	public virtual void Lock()
	{
		m_DragLocked = !m_DragLocked;
	}

	private void BoundsChanged()
	{
		m_WorkspaceUI.vacuumHandle.transform.localPosition = outerBounds.center;
		m_WorkspaceUI.vacuumHandle.transform.localScale = outerBounds.size;
		m_WorkspaceUI.SetBounds(contentBounds);
	}

	protected abstract void OnBoundsChanged();
}