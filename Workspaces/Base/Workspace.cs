#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	abstract class Workspace : MonoBehaviour, IWorkspace, IInstantiateUI, IUsesStencilRef, IConnectInterfaces, IUsesViewerScale, ICustomActionMap
	{
		const float k_MaxFrameSize = 100f;
		public static readonly Vector3 k_DefaultBounds = new Vector3(0.7f, 0.4f, 0.4f);

		public const float HandleMargin = -0.15f; // Compensate for base size from frame model

		public event Action<IWorkspace> destroyed;

		protected WorkspaceUI m_WorkspaceUI;

		protected Vector3? m_CustomStartingBounds;

		public static readonly Vector3 k_MinBounds = new Vector3(0.55f, 0.4f, 0.1f);

		public Vector3 minBounds { get { return m_MinBounds; } set { m_MinBounds = value; } }
		[SerializeField]
		Vector3 m_MinBounds = k_MinBounds;

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
					size.x = Mathf.Clamp(Mathf.Max(size.x, minBounds.x), 0, k_MaxFrameSize);
					size.y = Mathf.Max(size.y, minBounds.y);
					size.z = Mathf.Clamp(Mathf.Max(size.z, minBounds.z), 0, k_MaxFrameSize);

					m_ContentBounds.size = size; //Only set size, ignore center.
					UpdateBounds();
					OnBoundsChanged();
				}
			}
		}
		Bounds m_ContentBounds;

		[SerializeField]
		GameObject m_BasePrefab;

		[SerializeField]
		ActionMap m_ActionMap;

		Coroutine m_VisibilityCoroutine;
		Coroutine m_ResetSizeCoroutine;

		readonly Dictionary<Transform, BaseHandle> m_HoveredHandles = new Dictionary<Transform, BaseHandle>();
		readonly Dictionary<Transform, DragState> m_DragStates = new Dictionary<Transform, DragState>();
		readonly List<Transform> m_DragsEnded = new List<Transform>();

		class DragState
		{
			public bool resizing { get; private set; }
			BaseHandle m_Handle;
			Vector3 m_PositionOffset;
			Quaternion m_RotationOffset;
			Workspace m_Workspace;
			Vector3 m_DragStart;
			Vector3 m_PositionStart;
			Vector3 m_BoundsSizeStart;

			public DragState(Workspace workspace, BaseHandle handle, Transform rayOrigin, bool resizing)
			{
				m_Workspace = workspace;
				m_Handle = handle;
				this.resizing = resizing;

				if (resizing)
				{
					m_DragStart = rayOrigin.position;
					m_PositionStart = workspace.transform.position;
					m_BoundsSizeStart = workspace.contentBounds.size;
				}
				else
				{
					MathUtilsExt.GetTransformOffset(rayOrigin, m_Workspace.transform, out m_PositionOffset, out m_RotationOffset);
				}
			}

			public void OnDragging(Transform rayOrigin)
			{
				if (resizing)
				{
					var viewerScale = m_Workspace.GetViewerScale();
					var dragVector = (rayOrigin.position - m_DragStart) / viewerScale;
					var bounds = m_Workspace.contentBounds;
					var transform = m_Workspace.transform;
					var workspaceUI = m_Workspace.m_WorkspaceUI;

					var positionOffsetForward = transform.forward * Vector3.Dot(dragVector, transform.forward) * 0.5f;
					var positionOffsetRight = transform.right * Vector3.Dot(dragVector, transform.right) * 0.5f;

					//if (m_Handle.Equals(workspaceUI.frontLeftHandle))
					//{
					//	bounds.size = m_BoundsSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right)
					//		+ Vector3.back * Vector3.Dot(dragVector, transform.forward);
					//}

					//if (m_Handle.Equals(workspaceUI.backLeftHandle))
					//{
					//	bounds.size = m_BoundsSizeStart + Vector3.left * Vector3.Dot(dragVector, transform.right)
					//		+ Vector3.forward * Vector3.Dot(dragVector, transform.forward);
					//}

					//if (m_Handle.Equals(workspaceUI.frontRightHandle))
					//{
					//	bounds.size = m_BoundsSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right)
					//		+ Vector3.back * Vector3.Dot(dragVector, transform.forward);
					//}

					//if (m_Handle.Equals(workspaceUI.backRightHandle))
					//{
					//	var size = m_BoundsSizeStart + Vector3.right * Vector3.Dot(dragVector, transform.right)
					//		+ Vector3.forward * Vector3.Dot(dragVector, transform.forward);
					//	bounds.size = size;
					//}

					m_Workspace.contentBounds = bounds;

					var positionOffset = Vector3.zero;
					if (Mathf.Approximately(m_Workspace.contentBounds.size.x, bounds.size.x))
						positionOffset += positionOffsetRight;

					if (Mathf.Approximately(m_Workspace.contentBounds.size.z, bounds.size.z))
						positionOffset += positionOffsetForward;

					m_Workspace.transform.position = m_PositionStart + positionOffset * viewerScale;

					m_PositionOffset = rayOrigin.position;
				}
				else
				{
					MathUtilsExt.SetTransformOffset(rayOrigin, m_Workspace.transform, m_PositionOffset, m_RotationOffset);
				}
			}
		}

		public Bounds outerBounds
		{
			get
			{
				const float kOuterBoundsCenterOffset = 0.225f; //Amount to lower the center of the outerBounds for better interaction with menus
				return new Bounds(contentBounds.center + Vector3.down * kOuterBoundsCenterOffset,
					new Vector3(
						contentBounds.size.x,
						contentBounds.size.y,
						contentBounds.size.z
						));
			}
		}

		public Bounds vacuumBounds { get { return outerBounds; } }

		public byte stencilRef { get; set; }

		/// <summary>
		/// If true, allow the front face of the workspace to dynamically adjust its angle when rotated
		/// </summary>
		public bool dynamicFaceAdjustment { set { m_WorkspaceUI.dynamicFaceAdjustment = value; } }

		/// <summary>
		/// If true, prevent the resizing of a workspace via the front and back resize handles
		/// </summary>
		public bool preventResize { set { m_WorkspaceUI.preventResize = value; } }

		/// <summary>
		/// (-1 to 1) ranged value that controls the separator mask's X-offset placement
		/// A value of zero will leave the mask in the center of the workspace
		/// </summary>
		public float topPanelDividerOffset
		{
			set
			{
				m_WorkspaceUI.topPanelDividerOffset = value;
				m_WorkspaceUI.bounds = contentBounds;
			}
		}

		public Transform topPanel { get { return m_WorkspaceUI.topPanel; } }

		public Transform frontPanel { get { return m_WorkspaceUI.frontPanel; } }

		public ActionMap actionMap { get { return m_ActionMap; } }

		public Transform leftRayOrigin { get; set; }
		public Transform rightRayOrigin { get; set; }

		public virtual void Setup()
		{
			var baseObject = this.InstantiateUI(m_BasePrefab);
			baseObject.transform.SetParent(transform, false);

			m_WorkspaceUI = baseObject.GetComponent<WorkspaceUI>();
			this.ConnectInterfaces(m_WorkspaceUI);
			m_WorkspaceUI.closeClicked += OnCloseClicked;
			m_WorkspaceUI.resetSizeClicked += OnResetClicked;

			m_WorkspaceUI.sceneContainer.transform.localPosition = Vector3.zero;

			//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
			m_ContentBounds = new Bounds(Vector3.up * k_DefaultBounds.y * 0.5f, m_CustomStartingBounds ?? k_DefaultBounds); // If custom bounds have been set, use them as the initial bounds
			UpdateBounds();

			foreach (var handle in m_WorkspaceUI.handles)
			{
				handle.hoverStarted += OnHandleHoverStarted;
				handle.hoverEnded += OnHandleHoverEnded;
			}

			this.StopCoroutine(ref m_VisibilityCoroutine);

			m_VisibilityCoroutine = StartCoroutine(AnimateShow());
		}

		void OnHandleHoverStarted(BaseHandle handle, HandleEventData eventData)
		{
			handle.GetComponent<Renderer>().enabled = true;
			m_HoveredHandles[eventData.rayOrigin] = handle;
		}

		void OnHandleHoverEnded(BaseHandle handle, HandleEventData eventData)
		{
			handle.GetComponent<Renderer>().enabled = false;

			var rayOrigin = eventData.rayOrigin;

			BaseHandle hovered;
			if (m_HoveredHandles.TryGetValue(rayOrigin, out hovered) && hovered == handle)
				m_HoveredHandles.Remove(rayOrigin);
		}

		public virtual void OnCloseClicked()
		{
			this.StopCoroutine(ref m_VisibilityCoroutine);

			m_VisibilityCoroutine = StartCoroutine(AnimateHide());
		}

		public virtual void OnResetClicked()
		{
			this.StopCoroutine(ref m_ResetSizeCoroutine);

			m_ResetSizeCoroutine = StartCoroutine(AnimateResetSize());
		}

		void UpdateBounds()
		{
			m_WorkspaceUI.bounds = contentBounds;
		}

		protected virtual void OnDestroy()
		{
			destroyed(this);
		}

		protected virtual void OnBoundsChanged()
		{
		}

		IEnumerator AnimateShow()
		{
			m_WorkspaceUI.highlightsVisible = true;

			var targetScale = transform.localScale;
			var scale = Vector3.zero;
			var smoothVelocity = Vector3.zero;
			var currentDuration = 0f;
			const float kTargetDuration = 0.75f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				transform.localScale = scale;
				scale = MathUtilsExt.SmoothDamp(scale, targetScale, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			transform.localScale = targetScale;

			m_WorkspaceUI.highlightsVisible = false;
			m_VisibilityCoroutine = null;
		}

		IEnumerator AnimateHide()
		{
			var targetScale = Vector3.zero;
			var scale = transform.localScale;
			var smoothVelocity = Vector3.zero;
			var currentDuration = 0f;
			const float kTargetDuration = 0.185f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				transform.localScale = scale;
				scale = MathUtilsExt.SmoothDamp(scale, targetScale, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			m_WorkspaceUI.highlightsVisible = false;
			m_VisibilityCoroutine = null;
			ObjectUtils.Destroy(gameObject);
		}

		IEnumerator AnimateResetSize()
		{
			var currentBoundsSize = contentBounds.size;
			var currentBoundsCenter = contentBounds.center;
			var targetBoundsSize = m_CustomStartingBounds ?? minBounds;
			var targetBoundsCenter = Vector3.zero;
			var smoothVelocitySize = Vector3.zero;
			var smoothVelocityCenter = Vector3.zero;
			var currentDuration = 0f;
			const float kTargetDuration = 0.75f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentBoundsCenter = MathUtilsExt.SmoothDamp(currentBoundsCenter, targetBoundsCenter, ref smoothVelocityCenter, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				currentBoundsSize = MathUtilsExt.SmoothDamp(currentBoundsSize, targetBoundsSize, ref smoothVelocitySize, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				contentBounds = new Bounds(currentBoundsCenter, currentBoundsSize);
				OnBoundsChanged();
				yield return null;
			}
		}

		public virtual void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var workspaceInput = (WorkspaceInput)input;
			var primaryLeft = workspaceInput.primaryLeft;
			var primaryRight = workspaceInput.primaryRight;
			var secondaryLeft = workspaceInput.secondaryLeft;
			var secondaryRight = workspaceInput.secondaryRight;
			foreach (var kvp in m_HoveredHandles)
			{
				var rayOrigin = kvp.Key;
				var handle = kvp.Value;

				if (rayOrigin == leftRayOrigin && primaryLeft.wasJustPressed
					|| rayOrigin == rightRayOrigin && primaryRight.wasJustPressed
					|| rayOrigin == leftRayOrigin && secondaryLeft.wasJustPressed
					|| rayOrigin == rightRayOrigin && secondaryRight.wasJustPressed)
				{
					m_DragStates[rayOrigin] = new DragState(this, handle, rayOrigin, true);
					// TODO: UI Feedback
				}
			}

			m_DragsEnded.Clear();
			foreach (var kvp in m_DragStates)
			{
				var rayOrigin = kvp.Key;
				var state = kvp.Value;

				if (!state.resizing && (rayOrigin == leftRayOrigin && primaryLeft.wasJustPressed || rayOrigin == rightRayOrigin && primaryRight.wasJustPressed)
					|| state.resizing && (rayOrigin == leftRayOrigin && secondaryLeft.wasJustPressed || rayOrigin == rightRayOrigin && secondaryRight.wasJustPressed))
				{
					m_DragsEnded.Add(rayOrigin);
				}
			}

			for (int i = 0; i < m_DragsEnded.Count; i++)
			{
				m_DragStates.Remove(m_DragsEnded[i]);
				// TODO: UI Feedback
			}

			foreach (var kvp in m_DragStates)
			{
				kvp.Value.OnDragging(kvp.Key);
			}
		}
	}
}
#endif
