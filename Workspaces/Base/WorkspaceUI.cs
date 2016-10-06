using System;
using UnityEngine.VR.Handles;
using UnityEngine.UI;

namespace UnityEngine.VR.Workspaces
{
	public class WorkspaceUI : MonoBehaviour
	{
		public event Action closeClicked = delegate { };
		public event Action lockClicked = delegate { };

		float m_OriginalUIContainerLocalYPos;
		float m_PreviousXRotation;
		float m_HandleScale;
		float m_FrontHandleYLocalPosition;
		float m_BackHandleYLocalPosition;
		float m_LeftHandleYLocalPosition;
		float m_RightHandleYLocalPosition;
		Material m_FrameGradientMaterial;
		Vector3 m_FrontResizeIconsContainerOriginalLocalPosition;
		Vector3 m_BaseFrontPanelRotation = Vector3.zero;
		Vector3 m_MaxFrontPanelRotation = new Vector3(45f, 0f, 0f);
		Vector3 m_OriginalFontPanelLocalPosition;
		Vector3 m_FrontResizeIconsContainerAngledLocalPosition;

		const float kMaxAlternateFrontPanelLocalZOffset = -0.075f;
		const float kMaxAlternateFrontPanelLocalYOffset = -0.005f;
		const int kAngledFaceBlendShapeIndex = 2;
		const int kHiddenFacesBlendShapeIndex = 3;
		const float backHandleOffset = -0.15f; // Offset to place the back handle in the expected region behind the workspace
		const float kPanelOffset = -0.09f; // The panel needs to be pulled back slightly

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

		[SerializeField]
		private SkinnedMeshRenderer m_Frame;

		[SerializeField]
		Transform m_FrameFrontFaceTransform;

		[SerializeField]
		Transform m_SeparatorMaskTransform;

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

		public bool dynamicFaceAdjustment { get; set; }

		/// <summary>
		/// (-1 to 1) ranged value that controls the separator mask's X-offset placement
		/// A value of zero will leave the mask in the center of the workspace
		/// </summary>
		public float signedSeparatorMaskOffset
		{
			set
			{
				m_SignedSeparatorMaskOffset = value;
				m_SeparatorMaskTransform.gameObject.SetActive(true);
			}
		}
		float? m_SignedSeparatorMaskOffset;

		public bool workspaceBaseInteractive
		{
			set
			{
				m_workspaceBaseInteractive = value;
				dynamicFaceAdjustment = false;

				if (m_workspaceBaseInteractive == false)
				{
					m_Frame.SetBlendShapeWeight(kHiddenFacesBlendShapeIndex, 100f);
					m_FrameFrontFaceTransform.gameObject.SetActive(false);
				}
			}
		}
		bool m_workspaceBaseInteractive = true;
		
		const float m_FaceWidthMatchMultiplier =  7.23f;

		public Bounds setBounds
		{
			get { return m_Bounds; }
			set
			{
				m_Bounds = value;

				// Because BlendShapes cap at 100, our workspace maxes out at 100m wide
				m_Frame.SetBlendShapeWeight(0, m_Bounds.size.x + Workspace.kHandleMargin);
				m_Frame.SetBlendShapeWeight(1, m_Bounds.size.z + Workspace.kHandleMargin);

				// Resize handles
				m_LeftHandle.transform.localPosition = new Vector3(-m_Bounds.extents.x + m_HandleScale * 0.5f, m_LeftHandleYLocalPosition, 0);
				m_LeftHandle.transform.localScale = new Vector3(m_Bounds.size.z, m_HandleScale, m_HandleScale);

				m_FrontHandle.transform.localPosition = new Vector3(0, m_FrontHandleYLocalPosition, -m_Bounds.extents.z - m_HandleScale);
				m_FrontHandle.transform.localScale = new Vector3(m_Bounds.size.x, m_HandleScale, m_HandleScale);

				m_RightHandle.transform.localPosition = new Vector3(m_Bounds.extents.x - m_HandleScale * 0.5f, m_RightHandleYLocalPosition, 0);
				m_RightHandle.transform.localScale = new Vector3(m_Bounds.size.z, m_HandleScale, m_HandleScale);

				m_BackHandle.transform.localPosition = new Vector3(0, m_BackHandleYLocalPosition, m_Bounds.extents.z - m_HandleScale - backHandleOffset);
				m_BackHandle.transform.localScale = new Vector3(m_Bounds.size.x, m_HandleScale, m_HandleScale);

				// Resize content container
				m_UIContentContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_Bounds.size.x);
				m_UIContentContainer.localPosition = new Vector3(0, m_OriginalUIContainerLocalYPos, -m_Bounds.extents.z);

				// Adjust front panel position if dynamic adjustment is enabled
				if (dynamicFaceAdjustment == false)
					m_FrontPanel.localPosition = new Vector3(0f, m_OriginalFontPanelLocalPosition.y, kPanelOffset);

				// Resize front panel
				m_FrameFrontFaceTransform.localScale = new Vector3(m_Bounds.size.x * m_FaceWidthMatchMultiplier, 1f, 1f);

				// Position the separator mask if enabled
				if (m_SignedSeparatorMaskOffset != null)
				{
					const float heightCompensationMultiplier = 4.225f;
					m_SeparatorMaskTransform.localPosition = new Vector3(m_Bounds.size.x * 0.5f * m_SignedSeparatorMaskOffset.Value, 0f, 0f);
					m_SeparatorMaskTransform.localScale = new Vector3(1f, 1f, m_Bounds.size.z * heightCompensationMultiplier);
				}

				m_GrabCollider.size = new Vector3(m_Bounds.size.x, m_GrabCollider.size.y, m_GrabCollider.size.z);
			}
		}
		private Bounds m_Bounds;

		private void ResizeHighlightBegin(BaseHandle baseHandle, HandleEventData eventData)
		{
			if (m_workspaceBaseInteractive == false)
				return;

			const float kOpacityTarget = 0.75f;
			const float kDuration = 0.5f;

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

		private void ResizeHighlightEnd(BaseHandle baseHandle, HandleEventData eventData)
		{
			if (m_workspaceBaseInteractive == false)
				return;

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

		private void Awake()
		{
			m_FrontHandle.hoverStarted += ResizeHighlightBegin;
			m_FrontHandle.hoverEnded += ResizeHighlightEnd;
			m_RightHandle.hoverStarted += ResizeHighlightBegin;
			m_RightHandle.hoverEnded += ResizeHighlightEnd;
			m_LeftHandle.hoverStarted += ResizeHighlightBegin;
			m_LeftHandle.hoverEnded += ResizeHighlightEnd;
			m_BackHandle.hoverStarted += ResizeHighlightBegin;
			m_BackHandle.hoverEnded += ResizeHighlightEnd;

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

			var handleTransform = leftHandle.transform;
			m_HandleScale = handleTransform.localScale.z;
			m_LeftHandleYLocalPosition = handleTransform.localPosition.y;
			m_RightHandleYLocalPosition = rightHandle.transform.localPosition.y;
			m_FrontHandleYLocalPosition = frontHandle.transform.localPosition.y;
			m_BackHandleYLocalPosition = backHandle.transform.localPosition.y;

			const float frontResizeIconsContainerforwardOffset = -0.025f;
			m_FrontResizeIconsContainerOriginalLocalPosition = m_FrontResizeIconsContainer.localPosition;
			m_FrontResizeIconsContainerAngledLocalPosition = new Vector3(m_FrontResizeIconsContainerOriginalLocalPosition.x, m_FrontResizeIconsContainerOriginalLocalPosition.y, m_FrontResizeIconsContainerOriginalLocalPosition.z + frontResizeIconsContainerforwardOffset);
		}

		void Update()
		{
			//m_FrameFrontFaceTransform.localScale = new Vector3(m_Bounds.size.x * m_FaceWidthMatchMultiplier, 1f, 1f); // hack remove

			if (dynamicFaceAdjustment == false)
				return;

			float currentXRotation = transform.rotation.eulerAngles.x;
			if (Mathf.Approximately(currentXRotation, m_PreviousXRotation))
				return; // Exit if no x rotation change occurred for this frame

			m_PreviousXRotation = currentXRotation;

			float angledAmount = Mathf.Clamp(Mathf.DeltaAngle(currentXRotation, 0f), 0f, 100f);
			float lerpAmount = angledAmount / 90f;
			m_FrontPanel.localRotation = Quaternion.Euler(Vector3.Lerp(m_BaseFrontPanelRotation, m_MaxFrontPanelRotation, lerpAmount));
			m_FrontPanel.localPosition = new Vector3(0f, Mathf.Lerp(m_OriginalFontPanelLocalPosition.y, kMaxAlternateFrontPanelLocalYOffset, lerpAmount), Mathf.Lerp(kPanelOffset, kMaxAlternateFrontPanelLocalZOffset, lerpAmount));

			m_Frame.SetBlendShapeWeight(kAngledFaceBlendShapeIndex, angledAmount);

			// offset the front resize icons to accommodate for the blendshape extending outwards
			m_FrontResizeIconsContainer.localPosition = Vector3.Lerp(m_FrontResizeIconsContainerOriginalLocalPosition, m_FrontResizeIconsContainerAngledLocalPosition, angledAmount * 0.1f);
		}

		public void CloseClick()
		{
			closeClicked();
		}

		public void LockClick()
		{
			lockClicked();
		}
	}
}