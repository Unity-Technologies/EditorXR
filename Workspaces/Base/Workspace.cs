#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	abstract class Workspace : MonoBehaviour, IWorkspace, IInstantiateUI, IUsesStencilRef, IConnectInterfaces, IUsesViewerScale
	{
		const float k_MaxFrameSize = 100f; // Because BlendShapes cap at 100, our workspace maxes out at 100m wide

		public static readonly Vector3 DefaultBounds = new Vector3(0.7f, 0.4f, 0.4f);
		public static readonly Vector3 MinBounds = new Vector3(0.55f, 0.4f, 0.1f);

		public const float FaceMargin = 0.025f;
		public const float HighlightMargin = 0.002f;

		[SerializeField]
		Vector3 m_MinBounds = MinBounds;

		[SerializeField]
		GameObject m_BasePrefab;

		[SerializeField]
		ActionMap m_ActionMap;

		Bounds m_ContentBounds;

		Coroutine m_VisibilityCoroutine;
		Coroutine m_ResetSizeCoroutine;

		protected WorkspaceUI m_WorkspaceUI;

		protected Vector3? m_CustomStartingBounds;

		public Vector3 minBounds { get { return m_MinBounds; } set { m_MinBounds = value; } }

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
		/// If true, prevent the resizing of a workspace
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

		public event Action<IWorkspace> destroyed;

		public Transform topPanel { get { return m_WorkspaceUI.topFaceContainer; } }

		public Transform frontPanel { get { return m_WorkspaceUI.frontPanel; } }

		public ActionMap actionMap { get { return m_ActionMap; } }

		public Transform leftRayOrigin { protected get; set; }
		public Transform rightRayOrigin { protected get; set; }

		public virtual void Setup()
		{
			var baseObject = this.InstantiateUI(m_BasePrefab);
			baseObject.transform.SetParent(transform, false);

			m_WorkspaceUI = baseObject.GetComponent<WorkspaceUI>();
			this.ConnectInterfaces(m_WorkspaceUI);
			m_WorkspaceUI.closeClicked += OnCloseClicked;
			m_WorkspaceUI.resetSizeClicked += OnResetClicked;

			m_WorkspaceUI.leftRayOrigin = leftRayOrigin;
			m_WorkspaceUI.rightRayOrigin = rightRayOrigin;

			m_WorkspaceUI.resize += bounds => { contentBounds = bounds; };

			m_WorkspaceUI.sceneContainer.transform.localPosition = Vector3.zero;

			//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
			m_ContentBounds = new Bounds(Vector3.up * DefaultBounds.y * 0.5f, m_CustomStartingBounds ?? DefaultBounds); // If custom bounds have been set, use them as the initial bounds
			UpdateBounds();

			this.StopCoroutine(ref m_VisibilityCoroutine);

			m_VisibilityCoroutine = StartCoroutine(AnimateShow());
		}

		public void Close()
		{
			this.StopCoroutine(ref m_VisibilityCoroutine);
			m_VisibilityCoroutine = StartCoroutine(AnimateHide());
		}

		protected virtual void OnCloseClicked()
		{
			Close();
		}

		protected virtual void OnResetClicked()
		{
			this.StopCoroutine(ref m_ResetSizeCoroutine);

			m_ResetSizeCoroutine = StartCoroutine(AnimateResetSize());
		}

		public void SetUIHighlightsVisible(bool value)
		{
			m_WorkspaceUI.highlightsVisible = value;
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

			var targetScale = Vector3.one;
			var scale = Vector3.zero;
			var smoothVelocity = Vector3.zero;
			var currentDuration = 0f;
			const float kTargetDuration = 0.75f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.deltaTime;
				transform.localScale = scale;
				scale = MathUtilsExt.SmoothDamp(scale, targetScale, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
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
				currentDuration += Time.deltaTime;
				transform.localScale = scale;
				scale = MathUtilsExt.SmoothDamp(scale, targetScale, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
				yield return null;
			}
			transform.localScale = targetScale;

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
				currentDuration += Time.deltaTime;
				currentBoundsCenter = MathUtilsExt.SmoothDamp(currentBoundsCenter, targetBoundsCenter, ref smoothVelocityCenter, kTargetDuration, Mathf.Infinity, Time.deltaTime);
				currentBoundsSize = MathUtilsExt.SmoothDamp(currentBoundsSize, targetBoundsSize, ref smoothVelocitySize, kTargetDuration, Mathf.Infinity, Time.deltaTime);
				contentBounds = new Bounds(currentBoundsCenter, currentBoundsSize);
				OnBoundsChanged();
				yield return null;
			}
		}

		public virtual void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			m_WorkspaceUI.ProcessInput((WorkspaceInput)input, consumeControl);
		}
	}
}
#endif
