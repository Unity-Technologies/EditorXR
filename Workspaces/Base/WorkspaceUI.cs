using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Manipulators;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Workspaces
{
	public class WorkspaceUI : MonoBehaviour, IUsesStencilRef
	{
		public event Action closeClicked = delegate {};
		public event Action resetSizeClicked = delegate {};

		const int kAngledFaceBlendShapeIndex = 2;
		const int kThinFrameBlendShapeIndex = 3;
		const float kFaceWidthMatchMultiplier =  7.1375f; // Multiplier that sizes the face to the intended width
		const float kBackResizeButtonPositionOffset = -0.02f; // Offset to place the back resize buttons in their intended location
		const float kPanelOffset = 0.0625f; // The panel needs to be pulled back slightly
		const string kMaterialStencilRef = "_StencilRef";

		// Cached for optimization
		float m_OriginalUIContainerLocalYPos;
		float m_PreviousXRotation;
		Vector3 m_OriginalFrontHandleLocalPosition;
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
		Coroutine m_TopFaceVisibleCoroutine;
		Material m_TopFaceMaterial;
		Material m_FrontFaceMaterial;

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
		BaseHandle m_MoveHandle;

		public Transform topFaceContainer { get { return m_TopFaceContainer; } }
		[SerializeField]
		Transform m_TopFaceContainer;

		public WorkspaceHighlight topHighlight { get { return m_TopHighlight; } }
		[SerializeField]
		WorkspaceHighlight m_TopHighlight;

		public bool dynamicFaceAdjustment { get { return m_DynamicFaceAdjustment; } set { m_DynamicFaceAdjustment = value; } }
		bool m_DynamicFaceAdjustment = true;

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
		GameObject m_ResetButton;

		[SerializeField]
		Transform m_TopHighlightContainer;

		[SerializeField]
		WorkspaceHighlight m_FrontHighlight;

		public bool highlightsVisible
		{
			set
			{
				if (m_TopHighlight.visible == value && m_FrontHighlight.visible == value)
					return;

				m_TopHighlight.visible = value;
				m_FrontHighlight.visible = value;

				this.StopCoroutine(ref m_FrameThicknessCoroutine);
				m_FrameThicknessCoroutine = value ? StartCoroutine(IncreaseFrameThickness()) : StartCoroutine(ResetFrameThickness());
			}
		}

		public bool frontHighlightVisible
		{
			set
			{
				if (m_FrontHighlight.visible == value)
					return;

				m_FrontHighlight.visible = value;

				this.StopCoroutine(ref m_FrameThicknessCoroutine);
				m_FrameThicknessCoroutine = !value ? StartCoroutine(ResetFrameThickness()) : StartCoroutine(IncreaseFrameThickness());
			}
		}

		public bool amplifyTopHighlight
		{
			set
			{
				this.StopCoroutine(ref m_TopFaceVisibleCoroutine);
				m_TopFaceVisibleCoroutine = value ? StartCoroutine(HideTopFace()) : StartCoroutine(ShowTopFace());
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

		public bool preventFrontBackResize
		{
			set
			{
				m_PreventFrontBackResize = value;
				if (value)
				{
					m_FrontHandleTransform.localScale = Vector3.zero;
					m_BackHandleTransform.localScale = Vector3.zero;
					m_FrontHandle.enabled = false;
					m_BackHandle.enabled = false;

					if (!m_PreventLeftRightResize) // Disable reset button if no resize handles are active
						m_ResetButton.SetActive(false);
				}
				else
				{
					m_FrontHandle.enabled = true;
					m_BackHandle.enabled = true;
					m_ResetButton.SetActive(true);
				}
			}
			private get { return m_PreventFrontBackResize; }
		}
		bool m_PreventFrontBackResize;

		public bool preventLeftRightResize
		{
			set
			{
				m_PreventLeftRightResize = value;
				if (value)
				{
					m_LeftHandleTransform.localScale = Vector3.zero;
					m_RightHandleTransform.localScale = Vector3.zero;
					m_LeftHandle.enabled = false;
					m_RightHandle.enabled = false;

					if (!m_PreventFrontBackResize) // Disable reset button if no resize handles are active
						m_ResetButton.SetActive(false);
				}
				else
				{
					m_LeftHandle.enabled = true;
					m_RightHandle.enabled = true;
					m_ResetButton.SetActive(true);
				}
			}
			private get { return m_PreventLeftRightResize; }
		}
		bool m_PreventLeftRightResize;

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

				// Resize content container
				m_UIContentContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boundsSize.x);
				m_UIContentContainer.localPosition = new Vector3(0, m_OriginalUIContainerLocalYPos, -extents.z);

				// Position the back resize handles
				m_BackResizeIconsContainer.localPosition = new Vector3 (m_BackResizeIconsContainerOriginalLocalPosition.x, m_BackResizeIconsContainerOriginalLocalPosition.y, boundsSize.z + kBackResizeButtonPositionOffset);

				// Adjust front panel position if dynamic adjustment is enabled
				if (!m_DynamicFaceAdjustment)
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

				// Scale the Top Face and the Top Face Highlight
				const float kHighlightDepthCompensation = 0.14f;
				const float kHighlightWidthCompensation = 0.01f;
				const float kTopFaceDepthCompensation = 0.144f;
				const float kTopFaceWidthCompensation = 0.014f;
				m_TopHighlightContainer.localScale = new Vector3(boundsSize.x - kHighlightWidthCompensation, 1f, boundsSize.z - kHighlightDepthCompensation);
				m_TopFaceContainer.localScale = new Vector3(boundsSize.x - kTopFaceWidthCompensation, 1f, boundsSize.z - kTopFaceDepthCompensation);
			}
		}
		Bounds m_Bounds;

		public byte stencilRef { get; set; }

		void ShowResizeUI(BaseHandle baseHandle, HandleEventData eventData)
		{
			this.StopCoroutine(ref m_FrameThicknessCoroutine);
			m_FrameThicknessCoroutine = StartCoroutine(IncreaseFrameThickness());

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
			this.StopCoroutine(ref m_FrameThicknessCoroutine);
			m_FrameThicknessCoroutine = StartCoroutine(ResetFrameThickness());

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

			m_OriginalFrontHandleLocalPosition = m_FrontHandleTransform.localPosition;

			const float frontResizeIconsContainerForwardOffset = -0.15f;
			const float frontResizeIconsContainerUpOffset = -0.025f;
			m_FrontResizeIconsContainerOriginalLocalPosition = m_FrontResizeIconsContainer.localPosition;
			m_BackResizeIconsContainerOriginalLocalPosition = m_BackResizeIconsContainer.localPosition;
			m_FrontResizeIconsContainerAngledLocalPosition = new Vector3(m_FrontResizeIconsContainerOriginalLocalPosition.x, m_FrontResizeIconsContainerOriginalLocalPosition.y + frontResizeIconsContainerUpOffset, m_FrontResizeIconsContainerOriginalLocalPosition.z + frontResizeIconsContainerForwardOffset);

			m_Frame.SetBlendShapeWeight(kThinFrameBlendShapeIndex, 50f); // Set default frame thickness to be in middle for a thinner initial frame

			if (m_TopPanelDividerOffset == null)
				m_TopPanelDividerTransform.gameObject.SetActive(false);
		}

		IEnumerator Start()
		{
			const string kShaderBlur = "_Blur";
			const string kShaderAlpha = "_Alpha";
			const string kShaderVerticalOffset = "_VerticalOffset";
			const float kTargetDuration = 1.25f;

			m_TopFaceMaterial = U.Material.GetMaterialClone(m_TopFaceContainer.GetComponentInChildren<MeshRenderer>());
			m_TopFaceMaterial.SetFloat("_Alpha", 1f);
			m_TopFaceMaterial.SetInt(kMaterialStencilRef, stencilRef);

			m_FrontFaceMaterial = U.Material.GetMaterialClone(m_FrameFrontFaceTransform.GetComponentInChildren<MeshRenderer>());
			m_FrontFaceMaterial.SetInt(kMaterialStencilRef, stencilRef);

			var originalBlurAmount = m_TopFaceMaterial.GetFloat("_Blur");
			var currentBlurAmount = 10f; // also the maximum blur amount
			var currentDuration = 0f;
			var currentVelocity = 0f;

			m_TopFaceMaterial.SetFloat(kShaderBlur, currentBlurAmount);
			m_TopFaceMaterial.SetFloat(kShaderVerticalOffset, 1f); // increase the blur sample offset to amplify the effect
			m_TopFaceMaterial.SetFloat(kShaderAlpha, 0.5f); // set partially transparent

			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentBlurAmount = U.Math.SmoothDamp(currentBlurAmount, originalBlurAmount, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_TopFaceMaterial.SetFloat(kShaderBlur, currentBlurAmount);

				float percentageComplete = currentDuration / kTargetDuration;
				m_TopFaceMaterial.SetFloat(kShaderVerticalOffset, 1 - percentageComplete); // lerp back towards an offset of zero
				m_TopFaceMaterial.SetFloat(kShaderAlpha, percentageComplete * 0.5f + 0.5f); // lerp towards fully opaque from 50% transparent

				yield return null;
			}

			m_TopFaceMaterial.SetFloat(kShaderBlur, originalBlurAmount);
			m_TopFaceMaterial.SetFloat(kShaderVerticalOffset, 0f);
			m_TopFaceMaterial.SetFloat(kShaderAlpha, 1f);

			yield return null;
		}

		void Update()
		{
			if (!m_DynamicFaceAdjustment)
				return;

			var currentXRotation = transform.rotation.eulerAngles.x;
			if (Mathf.Approximately(currentXRotation, m_PreviousXRotation))
				return; // Exit if no x rotation change occurred for this frame

			m_PreviousXRotation = currentXRotation;

			// a second additional value added to the y offset of the front panel when it is in mid-reveal,
			// lerped in at the middle of the rotation/reveal, and lerped out at the beginning & end of the rotation/reveal
			const float kCorrectiveMidFrontPanelLocalYOffset = 0.01f;
			const int kRevealCompensationBlendShapeIndex = 5;
			const float kMaxAlternateFrontPanelLocalZOffset = 0.0035f;
			const float kMaxAlternateFrontPanelLocalYOffset = 0.0325f;
			const float kLerpPadding = 1.2f; // pad lerp values increasingly as it increases, displaying the "front face reveal" sooner
			const float kCorrectiveRevealShapeMultiplier = 1.85f;
			var angledAmount = Mathf.Clamp(Mathf.DeltaAngle(currentXRotation, 0f), 0f, 90f);
			var midRevealCorrectiveShapeAmount = Mathf.PingPong(angledAmount * kCorrectiveRevealShapeMultiplier, 90);

			// blend between the target fully-revealed offset, and the rotationally mid-point-only offset for precise positioning of the front panel
			const float kMidRevealCorrectiveShapeMultiplier = 0.01f;
			var totalAlternateFrontPanelLocalYOffset = Mathf.Lerp(kMaxAlternateFrontPanelLocalYOffset, kCorrectiveMidFrontPanelLocalYOffset, midRevealCorrectiveShapeAmount * kMidRevealCorrectiveShapeMultiplier);
			// add lerp padding to reach and maintain the target value sooner
			var lerpAmount = (angledAmount / 90f) * kLerpPadding;

			// offset front panel according to workspace rotation angle
			const float kAdditionalFrontPanelLerpPadding = 1.1f;
			m_FrontPanel.localRotation = Quaternion.Euler(Vector3.Lerp(m_BaseFrontPanelRotation, m_MaxFrontPanelRotation, lerpAmount * kAdditionalFrontPanelLerpPadding));
			m_FrontPanel.localPosition = new Vector3(0f, Mathf.Lerp(m_OriginalFontPanelLocalPosition.y, totalAlternateFrontPanelLocalYOffset, lerpAmount), Mathf.Lerp(kPanelOffset, kMaxAlternateFrontPanelLocalZOffset, lerpAmount));

			// change blendshapes according to workspace rotation angle
			m_Frame.SetBlendShapeWeight(kAngledFaceBlendShapeIndex, angledAmount * kLerpPadding);
			m_Frame.SetBlendShapeWeight(kRevealCompensationBlendShapeIndex, midRevealCorrectiveShapeAmount);

			// offset the front resize icons to accommodate for the blendshape extending outwards
			m_FrontResizeIconsContainer.localPosition = Vector3.Lerp(m_FrontResizeIconsContainerOriginalLocalPosition, m_FrontResizeIconsContainerAngledLocalPosition, lerpAmount);

			// offset front handle position according to workspace rotation angle
			const float kBoundsZCompensation = -0.179f;
			var boundsZSize = m_Bounds.size.z;
			var frontHandleAngledPosition = new Vector3 (0f, -0.065f, -0.91f - (boundsZSize * kBoundsZCompensation));
			m_FrontHandleTransform.localPosition = Vector3.Lerp(m_OriginalFrontHandleLocalPosition, frontHandleAngledPosition, lerpAmount);
		}

		void OnDestroy()
		{
			U.Object.Destroy(m_TopFaceMaterial);
			U.Object.Destroy(m_FrontFaceMaterial);
		}

		public void CloseClick()
		{
			closeClicked();
		}

		public void ResetSizeClick()
		{
			resetSizeClicked();
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

		IEnumerator ShowTopFace()
		{
			const string kMaterialHighlightAlphaProperty = "_Alpha";
			const float kTargetAlpha = 1f;
			const float kTargetDuration = 0.35f;
			var currentDuration = 0f;
			var currentAlpha = m_TopFaceMaterial.GetFloat(kMaterialHighlightAlphaProperty);
			var currentVelocity = 0f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentAlpha = U.Math.SmoothDamp(currentAlpha, kTargetAlpha, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_TopFaceMaterial.SetFloat(kMaterialHighlightAlphaProperty, currentAlpha);
				yield return null;
			}

			m_TopFaceVisibleCoroutine = null;
		}

		IEnumerator HideTopFace()
		{
			const string kMaterialHighlightAlphaProperty = "_Alpha";
			const float kTargetAlpha = 0f;
			const float kTargetDuration = 0.2f;
			var currentDuration = 0f;
			var currentAlpha = m_TopFaceMaterial.GetFloat(kMaterialHighlightAlphaProperty);
			var currentVelocity = 0f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentAlpha = U.Math.SmoothDamp(currentAlpha, kTargetAlpha, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_TopFaceMaterial.SetFloat(kMaterialHighlightAlphaProperty, currentAlpha);
				yield return null;
			}

			m_TopFaceVisibleCoroutine = null;
		}
	}
}