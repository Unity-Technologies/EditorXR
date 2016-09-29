using System;
using System.Collections;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Workspaces
{
	public abstract class Workspace : MonoBehaviour, IInstantiateUI, IHighlight
	{
		public static readonly Vector3 kDefaultBounds = new Vector3(0.7f, 0.4f, 0.4f);
		public static readonly Vector3 kDefaultOffset = new Vector3(0, -0.15f, 1f);
		public static readonly Vector3 kVacuumOffset = new Vector3(0, -0.15f, 0.6f);
		public static readonly Quaternion kDefaultTilt = Quaternion.AngleAxis(-20, Vector3.right);

		public const float kHandleMargin = -0.15f; // Compensate for base size from frame model

		public event Action<Workspace> destroyed = delegate { };

		protected WorkspaceUI m_WorkspaceUI;

		public static readonly Vector3 kMinBounds = new Vector3(0.7f, 0.4f, 0.1f);
		private const float kExtraHeight = 0.15f; //Extra space for frame model

		public Vector3 minBounds { get { return m_MinBounds; } set { m_MinBounds = value; } }
		[SerializeField]
		private Vector3 m_MinBounds = kMinBounds;

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
					size.x = Mathf.Max(size.x, minBounds.x);
					size.y = Mathf.Max(size.y, minBounds.y);
					size.z = Mathf.Max(size.z, minBounds.z);

					m_ContentBounds.size = size; //Only set size, ignore center.
					UpdateBounds();
					OnBoundsChanged();
				}
			}
		}
		private Bounds m_ContentBounds;

		[SerializeField]
		private float m_VacuumTime = 0.75f;

		[SerializeField]
		private GameObject m_BasePrefab;

		private Vector3 m_DragStart;
		private Vector3 m_PositionStart;
		private Vector3 m_BoundSizeStart;
		private bool m_Dragging;
		private bool m_DragLocked;
		private bool m_Vacuuming;

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

		public Func<GameObject, GameObject> instantiateUI { protected get; set; }

		public Action<GameObject, bool> setHighlight { get; set; }

		public bool vacuumEnabled { set { m_WorkspaceUI.vacuumHandle.gameObject.SetActive(value); } }

		public virtual void Setup()
		{
			GameObject baseObject = instantiateUI(m_BasePrefab);
			baseObject.transform.SetParent(transform, false);

			m_WorkspaceUI = baseObject.GetComponent<WorkspaceUI>();
			m_WorkspaceUI.closeClicked += OnCloseClicked;
			m_WorkspaceUI.lockClicked += OnLockClicked;
			m_WorkspaceUI.sceneContainer.transform.localPosition = Vector3.zero;

			//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
			m_ContentBounds = new Bounds(Vector3.up * kDefaultBounds.y * 0.5f, kDefaultBounds);
			UpdateBounds();

			//Set up DirectManipulaotr
			var directManipulator = m_WorkspaceUI.directManipulator;
			directManipulator.target = transform;
			directManipulator.translate = Translate;
			directManipulator.rotate = Rotate;

			m_WorkspaceUI.vacuumHandle.doubleClick += OnDoubleClick;
			m_WorkspaceUI.vacuumHandle.hoverStarted += OnHandleHoverStarted;
			m_WorkspaceUI.vacuumHandle.hoverEnded += OnHandleHoverEnded;

			var handles = new BaseHandle[]
			{
				m_WorkspaceUI.leftHandle,
				m_WorkspaceUI.frontHandle,
				m_WorkspaceUI.backHandle,
				m_WorkspaceUI.rightHandle
			};

			foreach (var handle in handles)
			{
				handle.dragStarted += OnHandleDragStarted;
				handle.dragging += OnHandleDragging;
				handle.dragEnded += OnHandleDragEnded;

				handle.hoverStarted += OnHandleHoverStarted;
				handle.hoverEnded += OnHandleHoverEnded;
			}
		}

		public virtual void OnHandleDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
		{
			m_PositionStart = transform.position;
			m_DragStart = eventData.rayOrigin.position;
			m_BoundSizeStart = contentBounds.size;
			m_Dragging = true;
		}

		public virtual void OnHandleDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
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
				if (contentBounds.size == bounds.size) //Don't reposition if we hit minimum bounds
					transform.position = m_PositionStart + positionOffset;
			}
		}

		public virtual void OnHandleDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
		{
			m_Dragging = false;
		}

		public virtual void OnHandleHoverStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
		{
			if (handle == m_WorkspaceUI.vacuumHandle || !m_DragLocked)
				setHighlight(handle.gameObject, true);
		}

		public virtual void OnHandleHoverEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
		{
			if (handle == m_WorkspaceUI.vacuumHandle || !m_DragLocked)
				setHighlight(handle.gameObject, false);
		}

		private void OnDoubleClick(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
		{
			if (!m_Vacuuming)
				StartCoroutine(VacuumToViewer());
		}

		private void Translate(Vector3 deltaPosition)
		{
			if (m_DragLocked)
				return;

			transform.position += deltaPosition;
		}

		private void Rotate(Quaternion deltaRotation)
		{
			if (m_DragLocked)
				return;

			transform.rotation *= deltaRotation;
		}

		private IEnumerator VacuumToViewer()
		{
			m_Vacuuming = true;
			float startTime = Time.realtimeSinceStartup;
			Vector3 startPosition = transform.position;
			Quaternion startRotation = transform.rotation;

			Transform camera = U.Camera.GetMainCamera().transform;
			var cameraYawVector = camera.forward;
			cameraYawVector.y = 0;
			var cameraYaw = Quaternion.LookRotation(cameraYawVector, Vector3.up);

			Vector3 destPosition = camera.position + cameraYaw * kVacuumOffset;

			Quaternion destRotation = cameraYaw * kDefaultTilt;

			while (Time.realtimeSinceStartup < startTime + m_VacuumTime)
			{
				transform.position = Vector3.Lerp(startPosition, destPosition, (Time.realtimeSinceStartup - startTime) / m_VacuumTime);
				transform.rotation = Quaternion.Lerp(startRotation, destRotation, (Time.realtimeSinceStartup - startTime) / m_VacuumTime);
				yield return null;
			}

			transform.position = destPosition;
			transform.rotation = destRotation;
			m_Vacuuming = false;
		}

		public virtual void OnCloseClicked()
		{
			U.Object.Destroy(gameObject);
		}

		public virtual void OnLockClicked()
		{
			m_DragLocked = !m_DragLocked;
		}

		private void UpdateBounds()
		{
			m_WorkspaceUI.vacuumHandle.transform.localPosition = outerBounds.center;
			m_WorkspaceUI.vacuumHandle.transform.localScale = outerBounds.size;
			m_WorkspaceUI.SetBounds(contentBounds);
		}

		protected virtual void OnDestroy()
		{
			destroyed(this);
		}

		protected abstract void OnBoundsChanged();
	}
}