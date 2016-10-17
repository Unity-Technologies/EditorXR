using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Extensions;
using UnityEngine.VR.Handles;
using UnityEngine.VR.UI;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Workspaces
{
	public class WorkspaceUI : MonoBehaviour
	{
		public event Action closeClicked = delegate { };
		public event Action lockClicked = delegate { };

		const float kMaxAlternateFrontPanelLocalZOffset = -0.015f;
		const float kMaxAlternateFrontPanelLocalYOffset = 0.0525f;
		const int kAngledFaceBlendShapeIndex = 2;
		const int kThinFrameBlendShapeIndex = 3;
		const int kHiddenFacesBlendShapeIndex = 4;
		const float kFaceWidthMatchMultiplier =  7.1375f; // Multiplier that sizes the face to the intended width
		const float kBackResizeButtonPositionOffset = -0.02f; // Offset to place the back resize buttons in their intended location
		const float kBackHandleOffset = -0.145f; // Offset to place the back handle in the expected region behind the workspace
		const float kSideHandleOffset = 0.05f; // Offset to place the back handle in the expected region behind the workspace
		const float kPanelOffset = 0.0625f; // The panel needs to be pulled back slightly

		// Cached for optimization
		float m_OriginalUIContainerLocalYPos;
		float m_PreviousXRotation;
		float m_HandleScale;
		float m_FrontHandleYLocalPosition;
		float m_BackHandleYLocalPosition;
		float m_LeftHandleYLocalPosition;
		float m_RightHandleYLocalPosition;
		float m_AngledFrontHandleOffset;
		Material m_FrameGradientMaterial;
		Vector3 m_FrontResizeIconsContainerOriginalLocalPosition;
		Vector3 m_BackResizeIconsContainerOriginalLocalPosition;
		Vector3 m_BaseFrontPanelRotation = Vector3.zero;
		Vector3 m_MaxFrontPanelRotation = new Vector3(90f, 0f, 0f);
		Vector3 m_OriginalFontPanelLocalPosition;
		Vector3 m_FrontResizeIconsContainerAngledLocalPosition;
		Transform m_LeftHandleTransform;
		Transform m_RightHandleTransform;
		Transform m_FrontHandleTransform;
		Transform m_BackHandleTransform;
		Transform m_TopHighlightTransform;
		Coroutine m_RotateFrontFaceForwardCoroutine;
		Coroutine m_RotateFrontFaceBackwardCoroutine;
		Coroutine m_FrameThicknessCoroutine;

		public Transform sceneContainer { get { return m_SceneContainer; } }
		[SerializeField]
		private Transform m_SceneContainer;

		public RectTransform frontPanel { get { return m_FrontPanel; } }
		[SerializeField]
		private RectTransform m_FrontPanel;

		public DirectManipulator directManipulator { get { return m_DirectManipulator; } }
		[SerializeField]
		private DirectManipulator m_DirectManipulator;

		[SerializeField]
		private BoxCollider m_GrabCollider;

		public BaseHandle vacuumHandle { get { return m_VacuumHandle; } }
		[SerializeField]
		private BaseHandle m_VacuumHandle;

		public BaseHandle leftHandle { get { return m_LeftHandle; } }
		[SerializeField]
		private BaseHandle m_LeftHandle;

		public BaseHandle frontHandle { get { return m_FrontHandle; } }
		[SerializeField]
		private BaseHandle m_FrontHandle;

		public BaseHandle rightHandle { get { return m_RightHandle; } }
		[SerializeField]
		private BaseHandle m_RightHandle;

		public BaseHandle backHandle { get { return m_BackHandle; } }
		[SerializeField]
		private BaseHandle m_BackHandle;

		public BaseHandle moveHandle { get { return m_MoveHandle; } }
		[SerializeField]
		private BaseHandle m_MoveHandle;

		[SerializeField]
		private SkinnedMeshRenderer m_Frame;

		[SerializeField]
		Transform m_FrameFrontFaceTransform;

		[SerializeField]
		Transform m_TopPanelDividerTransform;

		[SerializeField]
		RectTransform m_UIContentContainer;

		[SerializeField]
		Image m_FrontLeftResizeIcon;

		[SerializeField]
		Image m_FrontRightResizeIcon;

		[SerializeField]
		Image m_BackLeftResizeIcon;

		[SerializeField]
		Image m_BackRightResizeIcon;

		[SerializeField]
		Image m_LeftSideFrontResizeIcon;

		[SerializeField]
		Image m_LeftSideBackResizeIcon;

		[SerializeField]
		Image m_RightSideFrontResizeIcon;

		[SerializeField]
		Image m_RightSideBackResizeIcon;

		[SerializeField]
		Transform m_FrontResizeIconsContainer;

		[SerializeField]
		Transform m_BackResizeIconsContainer;

		[SerializeField]
		WorkspaceHighlight m_TopHighlight;

		[SerializeField]
		Transform m_TopHighlightContainer;

		[SerializeField]
		WorkspaceHighlight m_FrontHighlight;

		public bool dynamicFaceAdjustment { get; set; }

		public bool highlightsVisible
		{
			set
			{
				if (m_TopHighlight.visible == value) // All highlights will be set with this value; only need to check one highlight visibility
					return;

				m_TopHighlight.visible = value;
				m_FrontHighlight.visible = value;

				StopCoroutine(ref m_FrameThicknessCoroutine);
				m_FrameThicknessCoroutine = value == false ? StartCoroutine(ResetFrameThickness()) : StartCoroutine(IncreaseFrameThickness());
			}
		}

		public bool frontHighlightVisible
		{
			set
			{
				if (m_FrontHighlight.visible == value)
					return;

				m_FrontHighlight.visible = value;

				StopCoroutine(ref m_FrameThicknessCoroutine);
				m_FrameThicknessCoroutine = value == false ? StartCoroutine(ResetFrameThickness()) : StartCoroutine(IncreaseFrameThickness());
			}
		}

		/// <summary>
		/// (-1 to 1) ranged value that controls the separator mask's X-offset placement
		/// A value of zero will leave the mask in the center of the workspace
		/// </summary>
		public float topPanelDividerOffset
		{
			set
			{
				m_TopPanelDividerOffset = value;
				m_TopPanelDividerTransform.gameObject.SetActive(true);
			}
		}
		float? m_TopPanelDividerOffset;

		public bool preventFrontBackResize { set; private get; }

		public Bounds bounds
		{
			get { return m_Bounds; }
			set
			{
				m_Bounds = value;
				var extents = m_Bounds.extents;
				var boundsSize = m_Bounds.size;

				// Because BlendShapes cap at 100, our workspace maxes out at 100m wide
				m_Frame.SetBlendShapeWeight(0, boundsSize.x + Workspace.kHandleMargin);
				m_Frame.SetBlendShapeWeight(1, boundsSize.z + Workspace.kHandleMargin);

				// Resize handles
				m_LeftHandleTransform.localPosition = new Vector3(-extents.x + m_HandleScale * 0.5f - kSideHandleOffset, m_LeftHandleYLocalPosition, 0);
				m_LeftHandleTransform.localScale = new Vector3(boundsSize.z, m_HandleScale, m_HandleScale);

				m_FrontHandleTransform.localScale = preventFrontBackResize == false ? new Vector3(boundsSize.x, m_HandleScale, m_HandleScale) : Vector3.zero;

				m_RightHandleTransform.localPosition = new Vector3(extents.x - m_HandleScale * 0.5f + kSideHandleOffset, m_RightHandleYLocalPosition, 0);
				m_RightHandleTransform.localScale = new Vector3(boundsSize.z, m_HandleScale, m_HandleScale);

				m_BackHandleTransform.localPosition = new Vector3(0, m_BackHandleYLocalPosition, extents.z - m_HandleScale - kBackHandleOffset);
				m_BackHandleTransform.localScale = preventFrontBackResize == false ? new Vector3(boundsSize.x, m_HandleScale, m_HandleScale) : Vector3.zero;

				// Resize content container
				m_UIContentContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boundsSize.x);
				m_UIContentContainer.localPosition = new Vector3(0, m_OriginalUIContainerLocalYPos, -extents.z);

				// Position the back resize handles
				m_BackResizeIconsContainer.localPosition = new Vector3 (m_BackResizeIconsContainerOriginalLocalPosition.x, m_BackResizeIconsContainerOriginalLocalPosition.y, boundsSize.z + kBackResizeButtonPositionOffset);

				// Adjust front panel position if dynamic adjustment is enabled
				if (dynamicFaceAdjustment == false)
					m_FrontPanel.localPosition = new Vector3(0f, m_OriginalFontPanelLocalPosition.y, kPanelOffset);

				// Resize front panel
				m_FrameFrontFaceTransform.localScale = new Vector3(boundsSize.x * kFaceWidthMatchMultiplier, 1f, 1f);

				// Position the separator mask if enabled
				if (m_TopPanelDividerOffset != null)
				{
					const float kDepthCompensation = 0.1375f;
					m_TopPanelDividerTransform.localPosition = new Vector3(boundsSize.x * 0.5f * m_TopPanelDividerOffset.Value, 0f, 0f);
					m_TopPanelDividerTransform.localScale = new Vector3(1f, 1f, boundsSize.z - kDepthCompensation);
				}

				var grabColliderSize = m_GrabCollider.size;
				m_GrabCollider.size = new Vector3(boundsSize.x, grabColliderSize.y, grabColliderSize.z);

				const float kHighlightDepthCompensation = 0.14f;
				const float kHighlightWidthCompensation = 0.01f;
				m_TopHighlightContainer.localScale = new Vector3(boundsSize.x - kHighlightWidthCompensation, 1f, boundsSize.z - kHighlightDepthCompensation);
			}
		}
		Bounds m_Bounds;

		void ShowResizeUI(BaseHandle baseHandle, HandleEventData eventData)
		{
			const float kOpacityTarget = 0.75f;
			const float kDuration = 0.25f;

			if (baseHandle == m_FrontHandle) // in order of potential usage
			{
				m_FrontLeftResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
				m_FrontRightResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			}
			else if (baseHandle == m_RightHandle)
			{
				m_RightSideFrontResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
				m_RightSideBackResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			}
			else if (baseHandle == m_LeftHandle)
			{
				m_LeftSideFrontResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
				m_LeftSideBackResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			}
			else if (baseHandle == m_BackHandle)
			{
				m_BackLeftResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
				m_BackRightResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			}
		}

		void HideResizeUI(BaseHandle baseHandle, HandleEventData eventData)
		{
			const float kOpacityTarget = 0f;
			const float kDuration = 0.2f;

			if (baseHandle == m_FrontHandle) // in order of potential usage
			{
				m_FrontLeftResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
				m_FrontRightResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			}
			else if (baseHandle == m_RightHandle)
			{
				m_RightSideFrontResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
				m_RightSideBackResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			}
			else if (baseHandle == m_LeftHandle)
			{
				m_LeftSideFrontResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
				m_LeftSideBackResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			}
			else if (baseHandle == m_BackHandle)
			{
				m_BackLeftResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
				m_BackRightResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			}
		}

		void Awake()
		{
			m_FrontHandle.hoverStarted += ShowResizeUI;
			m_FrontHandle.hoverEnded += HideResizeUI;
			m_RightHandle.hoverStarted += ShowResizeUI;
			m_RightHandle.hoverEnded += HideResizeUI;
			m_LeftHandle.hoverStarted += ShowResizeUI;
			m_LeftHandle.hoverEnded += HideResizeUI;
			m_BackHandle.hoverStarted += ShowResizeUI;
			m_BackHandle.hoverEnded += HideResizeUI;

			m_FrontLeftResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_FrontRightResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_RightSideFrontResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_RightSideBackResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_LeftSideFrontResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_LeftSideBackResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_BackLeftResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_BackRightResizeIcon.CrossFadeAlpha(0f, 0f, true);

			m_OriginalUIContainerLocalYPos = m_UIContentContainer.localPosition.y;
			m_OriginalFontPanelLocalPosition = m_FrontPanel.localPosition;

			m_LeftHandleTransform = m_LeftHandle.transform;
			m_RightHandleTransform = m_RightHandle.transform;
			m_FrontHandleTransform = m_FrontHandle.transform;
			m_BackHandleTransform = m_BackHandle.transform;

			m_HandleScale = m_LeftHandleTransform.localScale.z;
			m_LeftHandleYLocalPosition = m_LeftHandleTransform.localPosition.y;
			m_RightHandleYLocalPosition = m_LeftHandleYLocalPosition; // use the same for right as was used for left; front and back can differ
			m_FrontHandleYLocalPosition = m_FrontHandleTransform.localPosition.y;
			m_BackHandleYLocalPosition = m_BackHandleTransform.localPosition.y;

			const float frontResizeIconsContainerForwardOffset = -0.15f;
			const float frontResizeIconsContainerUpOffset = -0.025f;
			m_FrontResizeIconsContainerOriginalLocalPosition = m_FrontResizeIconsContainer.localPosition;
			m_BackResizeIconsContainerOriginalLocalPosition = m_BackResizeIconsContainer.localPosition;
			m_FrontResizeIconsContainerAngledLocalPosition = new Vector3(m_FrontResizeIconsContainerOriginalLocalPosition.x, m_FrontResizeIconsContainerOriginalLocalPosition.y + frontResizeIconsContainerUpOffset, m_FrontResizeIconsContainerOriginalLocalPosition.z + frontResizeIconsContainerForwardOffset);

			m_Frame.SetBlendShapeWeight(kThinFrameBlendShapeIndex, 50f); // Set default frame thickness to be in middle for a thinner initial frame

			if (m_TopPanelDividerOffset == null)
				m_TopPanelDividerTransform.gameObject.SetActive(false);
		}

		void Update()
		{
			if (dynamicFaceAdjustment == false)
				return;

			var currentXRotation = transform.rotation.eulerAngles.x;
			if (Mathf.Approximately(currentXRotation, m_PreviousXRotation))
				return; // Exit if no x rotation change occurred for this frame

			m_PreviousXRotation = currentXRotation;

			//var angledAmount = Mathf.Clamp(Mathf.DeltaAngle(currentXRotation, 0f), 0f, 120f);
			
			var angledAmount = Mathf.Clamp(Mathf.DeltaAngle(currentXRotation, 0f), 0f, 100f);
			var lerpAmount = angledAmount / 90f;
			m_FrontPanel.localRotation = Quaternion.Euler(Vector3.Lerp(m_BaseFrontPanelRotation, m_MaxFrontPanelRotation, lerpAmount));
			m_FrontPanel.localPosition = new Vector3(0f, Mathf.Lerp(m_OriginalFontPanelLocalPosition.y, kMaxAlternateFrontPanelLocalYOffset, lerpAmount), Mathf.Lerp(kPanelOffset, kMaxAlternateFrontPanelLocalZOffset, lerpAmount));

			m_Frame.SetBlendShapeWeight(kAngledFaceBlendShapeIndex, angledAmount);

			// offset the front resize icons to accommodate for the blendshape extending outwards
			const float blendShapeToLerpConversionFactor = 0.1f;
			m_AngledFrontHandleOffset = Mathf.Lerp(0f, 0.125f, blendShapeToLerpConversionFactor);
			m_FrontResizeIconsContainer.localPosition = Vector3.Lerp(m_FrontResizeIconsContainerOriginalLocalPosition, m_FrontResizeIconsContainerAngledLocalPosition, angledAmount * blendShapeToLerpConversionFactor);
			m_FrontHandleTransform.localPosition = new Vector3(0, m_FrontHandleYLocalPosition, -m_Bounds.extents.z - m_HandleScale + m_AngledFrontHandleOffset);
		}

		public void CloseClick()
		{
			closeClicked();
		}

		public void LockClick()
		{
			lockClicked();
		}

		IEnumerator IncreaseFrameThickness()
		{
			const float kTargetBlendAmount = 0f;
			const float kTargetDuration = 0.5f;
			var currentDuration = 0f;
			var currentBlendAmount = m_Frame.GetBlendShapeWeight(kThinFrameBlendShapeIndex);
			var currentVelocity = 0f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentBlendAmount = U.Math.SmoothDamp(currentBlendAmount, kTargetBlendAmount, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_Frame.SetBlendShapeWeight(kThinFrameBlendShapeIndex, currentBlendAmount);
				yield return null;
			}

			m_FrameThicknessCoroutine = null;
		}

		IEnumerator ResetFrameThickness()
		{
			const float kTargetBlendAmount = 50f;
			const float kTargetDuration = 0.5f;
			var currentDuration = 0f;
			var currentBlendAmount = m_Frame.GetBlendShapeWeight(kThinFrameBlendShapeIndex);
			var currentVelocity = 0f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentBlendAmount = U.Math.SmoothDamp(currentBlendAmount, kTargetBlendAmount, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_Frame.SetBlendShapeWeight(kThinFrameBlendShapeIndex, currentBlendAmount);
				yield return null;
			}

			m_FrameThicknessCoroutine = null;
		}
	}
}