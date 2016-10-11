using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Extensions;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Workspaces
{
	public class WorkspaceUI : MonoBehaviour
	{
		public event Action closeClicked = delegate { };
		public event Action lockClicked = delegate { };

		const float kMaxAlternateFrontPanelLocalZOffset = -0.136f;
		const float kMaxAlternateFrontPanelLocalYOffset = 0.0525f;
		const int kAngledFaceBlendShapeIndex = 2;
		const int kThinFrameBlendShapeIndex = 3;
		const int kHiddenFacesBlendShapeIndex = 4;
		const float kFaceWidthMatchMultiplier =  7.23f; // Multiplier that sizes the face to the intended width
		const float kBackResizeButtonPositionOffset = 0.057f; // Offset to place the back resize buttons in their intended location
		const float kBackHandleOffset = -0.145f; // Offset to place the back handle in the expected region behind the workspace
		const float kSideHandleOffset = 0.05f; // Offset to place the back handle in the expected region behind the workspace
		const float kPanelOffset = -0.0495f; // The panel needs to be pulled back slightly

		// Cached for optimization
		float m_OriginalUIContainerLocalYPos;
		float m_PreviousXRotation;
		float m_HandleScale;
		float m_FrontHandleYLocalPosition;
		float m_BackHandleYLocalPosition;
		float m_LeftHandleYLocalPosition;
		float m_RightHandleYLocalPosition;
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
		Coroutine m_RotateFrontFaceForwardCoroutine;
		Coroutine m_RotateFrontFaceBackwardCoroutine;

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

		public bool dynamicFaceAdjustment { get; set; }

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

		public bool workspacePanelsVisible
		{
			set
			{
				m_workspacePanelsVisible = value;
				dynamicFaceAdjustment = false;

				if (m_workspacePanelsVisible == false)
				{
					m_Frame.SetBlendShapeWeight(kHiddenFacesBlendShapeIndex, 100f);
					m_FrameFrontFaceTransform.gameObject.SetActive(false);
				}
			}
		}
		bool m_workspacePanelsVisible = true;

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

				m_FrontHandleTransform.localPosition = new Vector3(0, m_FrontHandleYLocalPosition, -extents.z - m_HandleScale);
				m_FrontHandleTransform.localScale = new Vector3(boundsSize.x, m_HandleScale, m_HandleScale);

				m_RightHandleTransform.localPosition = new Vector3(extents.x - m_HandleScale * 0.5f + kSideHandleOffset, m_RightHandleYLocalPosition, 0);
				m_RightHandleTransform.localScale = new Vector3(boundsSize.z, m_HandleScale, m_HandleScale);

				m_BackHandleTransform.localPosition = new Vector3(0, m_BackHandleYLocalPosition, extents.z - m_HandleScale - kBackHandleOffset);
				m_BackHandleTransform.localScale = new Vector3(boundsSize.x, m_HandleScale, m_HandleScale);

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
					const float heightCompensationMultiplier = 4.225f;
					m_TopPanelDividerTransform.localPosition = new Vector3(boundsSize.x * 0.5f * m_TopPanelDividerOffset.Value, 0f, 0f);
					m_TopPanelDividerTransform.localScale = new Vector3(1f, 1f, boundsSize.z * heightCompensationMultiplier);
				}

				var grabColliderSize = m_GrabCollider.size;
				m_GrabCollider.size = new Vector3(boundsSize.x, grabColliderSize.y, grabColliderSize.z);
			}
		}
		Bounds m_Bounds;

		void ShowResizeUI(BaseHandle baseHandle, HandleEventData eventData)
		{
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

			const float frontResizeIconsContainerForwardOffset = -0.025f;
			const float frontResizeIconsContainerUpOffset = -0.025f;
			m_FrontResizeIconsContainerOriginalLocalPosition = m_FrontResizeIconsContainer.localPosition;
			m_BackResizeIconsContainerOriginalLocalPosition = m_BackResizeIconsContainer.localPosition;
			m_FrontResizeIconsContainerAngledLocalPosition = new Vector3(m_FrontResizeIconsContainerOriginalLocalPosition.x, m_FrontResizeIconsContainerOriginalLocalPosition.y + frontResizeIconsContainerUpOffset, m_FrontResizeIconsContainerOriginalLocalPosition.z + frontResizeIconsContainerForwardOffset);

			m_Frame.SetBlendShapeWeight(kThinFrameBlendShapeIndex, 50f); // Set default frame thickness to be in middle for a thinner initial frame
		}

		void Update()
		{
			if (dynamicFaceAdjustment == false)
				return;

			var currentXRotation = transform.rotation.eulerAngles.x;
			if (Mathf.Approximately(currentXRotation, m_PreviousXRotation))
				return; // Exit if no x rotation change occurred for this frame

			m_PreviousXRotation = currentXRotation;

			var angledAmount = Mathf.Clamp(Mathf.DeltaAngle(currentXRotation, 0f), 0f, 120f);
			if (angledAmount > 45f)
			{
				StopCoroutine(ref m_RotateFrontFaceBackwardCoroutine);

				if (m_RotateFrontFaceForwardCoroutine == null)
					m_RotateFrontFaceForwardCoroutine = StartCoroutine(RotateFrontFaceForward());
			}
			else
			{
				StopCoroutine(ref m_RotateFrontFaceForwardCoroutine);

				if (m_RotateFrontFaceBackwardCoroutine == null)
					m_RotateFrontFaceBackwardCoroutine = StartCoroutine(RotateFrontFaceBackward());
			}
		}

		public void CloseClick()
		{
			closeClicked();
		}

		public void LockClick()
		{
			lockClicked();
		}

		IEnumerator RotateFrontFaceForward()
		{
			const float targetBlendAmount = 100f;
			var currentBlendAmount = m_Frame.GetBlendShapeWeight(kAngledFaceBlendShapeIndex);
			var currentVelocity = 0f;
			while (currentBlendAmount < targetBlendAmount)
			{
				currentBlendAmount = U.Math.SmoothDamp(currentBlendAmount, targetBlendAmount, ref currentVelocity, 0.5f, Mathf.Infinity, Time.unscaledDeltaTime);
				m_Frame.SetBlendShapeWeight(kAngledFaceBlendShapeIndex, currentBlendAmount);

				var lerpAmount = currentBlendAmount / 100;
				m_FrontResizeIconsContainer.localPosition = Vector3.Lerp(m_FrontResizeIconsContainerOriginalLocalPosition, m_FrontResizeIconsContainerAngledLocalPosition, lerpAmount);
				m_FrontPanel.localRotation = Quaternion.Euler(Vector3.Lerp(m_BaseFrontPanelRotation, m_MaxFrontPanelRotation, lerpAmount));
				// offset the front resize icons to accommodate for the blendshape extending outwards
				m_FrontPanel.localPosition = new Vector3(0f, Mathf.Lerp(m_OriginalFontPanelLocalPosition.y, kMaxAlternateFrontPanelLocalYOffset, lerpAmount), Mathf.Lerp(kPanelOffset, kMaxAlternateFrontPanelLocalZOffset, lerpAmount));
				yield return null;
			}
		}

		IEnumerator RotateFrontFaceBackward()
		{
			const float targetBlendAmount = 0f;
			var currentBlendAmount = m_Frame.GetBlendShapeWeight(kAngledFaceBlendShapeIndex);
			var currentVelocity = 0f;
			while (currentBlendAmount > targetBlendAmount)
			{
				currentBlendAmount = U.Math.SmoothDamp(currentBlendAmount, targetBlendAmount, ref currentVelocity, 0.5f, Mathf.Infinity, Time.unscaledDeltaTime);
				m_Frame.SetBlendShapeWeight(kAngledFaceBlendShapeIndex, currentBlendAmount);

				var lerpAmount = currentBlendAmount / 50;
				m_FrontResizeIconsContainer.localPosition = Vector3.Lerp(m_FrontResizeIconsContainerOriginalLocalPosition, m_FrontResizeIconsContainerAngledLocalPosition, lerpAmount);
				m_FrontPanel.localRotation = Quaternion.Euler(Vector3.Lerp(m_BaseFrontPanelRotation, m_MaxFrontPanelRotation, lerpAmount));
				m_FrontPanel.localPosition = new Vector3(0f, Mathf.Lerp(m_OriginalFontPanelLocalPosition.y, kMaxAlternateFrontPanelLocalYOffset, lerpAmount), Mathf.Lerp(kPanelOffset, kMaxAlternateFrontPanelLocalZOffset, lerpAmount));
				yield return null;
			}
		}
	}
}