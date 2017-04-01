#if UNITY_EDITOR
#define debug
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
#if debug
	[ExecuteInEditMode]
#endif
	sealed class WorkspaceUI : MonoBehaviour, IUsesStencilRef
	{
#if debug
		public Bounds editorBounds;
		public float width, height, offset;
#endif

		public event Action closeClicked;
		public event Action resetSizeClicked;

		const int k_AngledFaceBlendShapeIndex = 2;
		const int k_ThinFrameBlendShapeIndex = 3;
		const string k_MaterialStencilRef = "_StencilRef";

		// Cached for optimization
		float m_PreviousXRotation;
		Vector3 m_BaseFrontPanelRotation = Vector3.zero;
		Vector3 m_MaxFrontPanelRotation = new Vector3(90f, 0f, 0f);
		Coroutine m_FrameThicknessCoroutine;
		Coroutine m_TopFaceVisibleCoroutine;
		Material m_TopFaceMaterial;
		Material m_FrontFaceMaterial;

		float m_LerpAmount;
		float m_HandleZOffset;

		public Transform sceneContainer { get { return m_SceneContainer; } }
		[SerializeField]
		Transform m_SceneContainer;

		public RectTransform frontPanel { get { return m_FrontPanel; } }
		[SerializeField]
		RectTransform m_FrontPanel;

		public Transform topPanel { get; private set; }

		public BaseHandle[] handles { get { return m_Handles; } }
		[SerializeField]
		BaseHandle[] m_Handles;

		public Transform leftHandle { get { return m_LeftHandle; } }
		[SerializeField]
		Transform m_LeftHandle;

		public Transform rightHandle { get { return m_RightHandle; } }
		[SerializeField]
		Transform m_RightHandle;

		public Transform backHandle { get { return m_BackHandle; } }
		[SerializeField]
		Transform m_BackHandle;

		public Transform frontTopHandle { get { return m_FrontTopHandle; } }
		[SerializeField]
		Transform m_FrontTopHandle;

		public Transform frontBottomHandle { get { return m_FrontBottomHandle; } }
		[SerializeField]
		Transform m_FrontBottomHandle;

		public Transform topFaceContainer { get { return m_TopFaceContainer; } }
		[SerializeField]
		Transform m_TopFaceContainer;

		public WorkspaceHighlight topHighlight { get { return m_TopHighlight; } }
		[SerializeField]
		WorkspaceHighlight m_TopHighlight;

		public bool dynamicFaceAdjustment { get { return m_DynamicFaceAdjustment; } set { m_DynamicFaceAdjustment = value; } }
		bool m_DynamicFaceAdjustment = true;

		[SerializeField]
		SkinnedMeshRenderer m_Frame;

		[SerializeField]
		Transform m_FrameFrontFaceTransform;

		[SerializeField]
		Transform m_FrameFrontFaceHighlightTransform;

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
		GameObject m_ResetButton;

		[SerializeField]
		Transform m_TopHighlightContainer;

		[SerializeField]
		WorkspaceHighlight m_FrontHighlight;

		[SerializeField]
		float m_CornerHandleSize = 0.05f;

		[SerializeField]
		float m_FrameHandleSize = 0.01f;

		[SerializeField]
		float m_FrameHeight = 0.09275f;

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

		public bool preventResize { get; set; }

		public Bounds bounds
		{
			get { return m_Bounds; }
			set
			{
				m_Bounds = value;
				var extents = m_Bounds.extents;
				var size = m_Bounds.size;

				// Because BlendShapes cap at 100, our workspace maxes out at 100m wide
				const float kWidthMultiplier = 0.9616f;
				const float kDepthMultiplier = 0.99385f;
				const float kWidthOffset = -0.165f;
				const float kDepthOffset = -0.038f;

				const float kFaceMargin = 0.025f;

				var width = size.x;
				var depth = size.z;
				var faceWidth = width - kFaceMargin;
				var faceDepth = depth - kFaceMargin;

				m_Frame.SetBlendShapeWeight(0, width * kWidthMultiplier + kWidthOffset);
				m_Frame.SetBlendShapeWeight(1, depth * kDepthMultiplier + kDepthOffset);

				// Resize content container
				m_UIContentContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, faceWidth);
				var localPosition = m_UIContentContainer.localPosition;
				localPosition.z = -extents.z;
				m_UIContentContainer.localPosition = localPosition;

				// Adjust front panel position if dynamic adjustment is enabled
				//if (!m_DynamicFaceAdjustment)
				//	m_FrontPanel.localPosition = new Vector3(0f, m_OriginalFontPanelLocalPosition.y, k_PanelOffset);

				// Resize front panel
				m_FrameFrontFaceTransform.localScale = new Vector3(faceWidth, 1f, 1f);
				const float kFrontFaceHighlightMargin = 0.0008f;
				m_FrameFrontFaceHighlightTransform.localScale = new Vector3(faceWidth + kFrontFaceHighlightMargin, 1f, 1f);

				// Position the separator mask if enabled
				if (m_TopPanelDividerOffset != null)
				{
					const float kDepthCompensation = 0.1375f;
					m_TopPanelDividerTransform.localPosition = new Vector3(size.x * 0.5f * m_TopPanelDividerOffset.Value, 0f, 0f);
					m_TopPanelDividerTransform.localScale = new Vector3(1f, 1f, size.z - kDepthCompensation);
				}

				// Scale the Top Face and the Top Face Highlight
				const float kHighlightMargin = 0.0005f;
				m_TopHighlightContainer.localScale = new Vector3(faceWidth + kHighlightMargin, 1f, faceDepth + kHighlightMargin);
				m_TopFaceContainer.localScale = new Vector3(faceWidth, 1f, faceDepth);

				UpdateHandles();
			}
		}

		void UpdateHandles()
		{
			var extents = m_Bounds.extents;
			var size = m_Bounds.size;
			var halfWidth = extents.x;
			var handleScaleX = size.x - m_FrameHandleSize;
			var handleScaleZ = size.z + m_FrameHandleSize + m_HandleZOffset;
			var halfHeight = -m_FrameHeight * 0.5f;
			var halfDepth = extents.z;
			var halfZOffset = m_HandleZOffset * -0.5f;
			var handleHeight = m_FrameHeight + m_FrameHandleSize;

			var transform = m_LeftHandle.transform;
			transform.localPosition = new Vector3(-halfWidth, halfHeight, halfZOffset);
			transform.localScale = new Vector3(m_FrameHandleSize, handleHeight, handleScaleZ);

			transform = m_RightHandle.transform;
			transform.localPosition = new Vector3(halfWidth, halfHeight, halfZOffset);
			transform.localScale = new Vector3(m_FrameHandleSize, handleHeight, handleScaleZ);

			transform = m_BackHandle.transform;
			transform.localPosition = new Vector3(0, halfHeight, halfDepth);
			transform.localScale = new Vector3(handleScaleX, handleHeight, m_FrameHandleSize);

			transform = m_FrontTopHandle.transform;
			transform.localPosition = new Vector3(0, 0, -halfDepth);
			transform.localScale = new Vector3(handleScaleX, m_FrameHandleSize, m_FrameHandleSize);

			transform = m_FrontBottomHandle.transform;
			var botHandleYPosition = -m_FrameHandleSize * 0.5f + (m_FrameHeight - m_FrameHandleSize * 0.5f) * (m_LerpAmount - 1);
			transform.localPosition = new Vector3(0, botHandleYPosition, -halfDepth - m_HandleZOffset);
			transform.localScale = new Vector3(handleScaleX, m_FrameHandleSize, m_FrameHandleSize);

			//foreach (var handle in m_RightHandle)
			//{
			//	transform = handle.transform;
			//	localPosition = transform.localPosition;
			//	localPosition.x = halfWidth;
			//	localPosition.z = halfZOffset;
			//	transform.localPosition = localPosition;

			//	transform.localScale = new Vector3(m_FrameHandleSize, m_FrameHandleSize, halfDepth * 2 - scaleOffset + m_HandleZOffset);
			//}

			//for (int i = 0; i < m_FrontTopHandle.Length; i++)
			//{
			//	var handle = m_FrontTopHandle[i];
			//	transform = handle.transform;
			//	localPosition = transform.localPosition;
			//	localPosition.z = -halfDepth - m_HandleZOffset;

			//	if (i == 1)
			//	{
			//		localPosition.y = -m_FrameHandleSize * 0.5f + (m_FrameHeight - m_FrameHandleSize * 0.5f) * (m_LerpAmount - 1);
			//		localPosition.z -= m_HandleZOffset;
			//	}

			//	transform.localPosition = localPosition;

			//	transform.localScale = new Vector3(halfWidth * 2 - scaleOffset, m_FrameHandleSize, m_FrameHandleSize);
			//}

			//foreach (var handle in m_BackHandle)
			//{
			//	transform = handle.transform;
			//	localPosition = transform.localPosition;
			//	localPosition.z = halfDepth;
			//	transform.localPosition = localPosition;

			//	transform.localScale = new Vector3(halfWidth * 2 - scaleOffset, m_FrameHandleSize, m_FrameHandleSize);
			//}
		}

		Bounds m_Bounds;

		public byte stencilRef { get; set; }

		void ShowResizeUI(BaseHandle handle, HandleEventData eventData)
		{
			handle.GetComponent<Renderer>().enabled = true;

			this.StopCoroutine(ref m_FrameThicknessCoroutine);
			m_FrameThicknessCoroutine = StartCoroutine(IncreaseFrameThickness());

			const float kOpacityTarget = 0.75f;
			const float kDuration = 0.25f;
			//if (handle == m_BackLeftHandle) // in order of potential usage
			//	m_BackLeftResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			//else if (handle == m_FrontRightHandle)
			//	m_FrontRightResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			//else if (handle == m_FrontLeftHandle)
			//	m_FrontLeftResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			//else if (handle == m_BackRightHandle)
			//	m_BackRightResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
		}

		void HideResizeUI(BaseHandle handle, HandleEventData eventData)
		{
			handle.GetComponent<Renderer>().enabled = false;

			this.StopCoroutine(ref m_FrameThicknessCoroutine);
			m_FrameThicknessCoroutine = StartCoroutine(ResetFrameThickness());

			const float kOpacityTarget = 0f;
			const float kDuration = 0.2f;
			//if (handle == m_BackLeftHandle) // in order of potential usage
			//	m_BackLeftResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			//else if (handle == m_FrontRightHandle)
			//	m_FrontRightResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			//else if (handle == m_FrontLeftHandle)
			//	m_FrontLeftResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
			//else if (handle == m_BackRightHandle)
			//	m_BackRightResizeIcon.CrossFadeAlpha(kOpacityTarget, kDuration, true);
		}

		void Awake()
		{
			//m_BackLeftHandle.hoverStarted += ShowResizeUI;
			//m_BackLeftHandle.hoverEnded += HideResizeUI;
			//m_FrontRightHandle.hoverStarted += ShowResizeUI;
			//m_FrontRightHandle.hoverEnded += HideResizeUI;
			//m_FrontLeftHandle.hoverStarted += ShowResizeUI;
			//m_FrontLeftHandle.hoverEnded += HideResizeUI;
			//m_BackRightHandle.hoverStarted += ShowResizeUI;
			//m_BackRightHandle.hoverEnded += HideResizeUI;

			m_FrontLeftResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_FrontRightResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_BackLeftResizeIcon.CrossFadeAlpha(0f, 0f, true);
			m_BackRightResizeIcon.CrossFadeAlpha(0f, 0f, true);

			foreach (var handle in handles)
			{
				handle.hoverStarted += OnHandleHoverStarted;
				handle.hoverEnded += OnHandleHoverEnded;
			}

			m_Frame.SetBlendShapeWeight(k_ThinFrameBlendShapeIndex, 50f); // Set default frame thickness to be in middle for a thinner initial frame

			if (m_TopPanelDividerOffset == null)
				m_TopPanelDividerTransform.gameObject.SetActive(false);

			topPanel = m_TopFaceContainer; // The TopFaceContainer serves as the transform that the workspace expects when fetching the TopPanel
		}

		static void OnHandleHoverEnded(BaseHandle handle, HandleEventData eventData)
		{
			handle.GetComponent<Renderer>().enabled = false;
		}

		static void OnHandleHoverStarted(BaseHandle handle, HandleEventData eventData)
		{
			handle.GetComponent<Renderer>().enabled = true;
		}

		IEnumerator Start()
		{
			const string kShaderBlur = "_Blur";
			const string kShaderAlpha = "_Alpha";
			const string kShaderVerticalOffset = "_VerticalOffset";
			const float kTargetDuration = 1.25f;

			m_TopFaceMaterial = MaterialUtils.GetMaterialClone(m_TopFaceContainer.GetComponentInChildren<MeshRenderer>());
			m_TopFaceMaterial.SetFloat("_Alpha", 1f);
			m_TopFaceMaterial.SetInt(k_MaterialStencilRef, stencilRef);

			m_FrontFaceMaterial = MaterialUtils.GetMaterialClone(m_FrameFrontFaceTransform.GetComponentInChildren<MeshRenderer>());
			m_FrontFaceMaterial.SetInt(k_MaterialStencilRef, stencilRef);

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
				currentBlurAmount = MathUtilsExt.SmoothDamp(currentBlurAmount, originalBlurAmount, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
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
#if debug
			bounds = editorBounds;
#endif

			if (!m_DynamicFaceAdjustment)
				return;

			var currentXRotation = transform.rotation.eulerAngles.x;
#if !debug
			if (Mathf.Approximately(currentXRotation, m_PreviousXRotation))
				return; // Exit if no x rotation change occurred for this frame
#endif

			m_PreviousXRotation = currentXRotation;

			// a second additional value added to the y offset of the front panel when it is in mid-reveal,
			// lerped in at the middle of the rotation/reveal, and lerped out at the beginning & end of the rotation/reveal
			const int kRevealCompensationBlendShapeIndex = 5;
			const float kLerpPadding = 1.2f; // pad lerp values increasingly as it increases, displaying the "front face reveal" sooner
			const float kCorrectiveRevealShapeMultiplier = 1.85f;
			var angledAmount = Mathf.Clamp(Mathf.DeltaAngle(currentXRotation, 0f), 0f, 90f);
			var midRevealCorrectiveShapeAmount = Mathf.PingPong(angledAmount * kCorrectiveRevealShapeMultiplier, 90);
			// add lerp padding to reach and maintain the target value sooner
			m_LerpAmount = (angledAmount / 90f) * kLerpPadding;

			// offset front panel according to workspace rotation angle
			const float kAdditionalFrontPanelLerpPadding = 1.1f;
			const float kFrontPanelYOffset = 0.03f;
			const float kFrontPanelZStartOffset = 0.0084f;
			const float kFrontPanelZEndOffset = -0.05f;
			m_FrontPanel.localRotation = Quaternion.Euler(Vector3.Lerp(m_BaseFrontPanelRotation, m_MaxFrontPanelRotation, m_LerpAmount * kAdditionalFrontPanelLerpPadding));
			m_FrontPanel.localPosition = Vector3.Lerp(Vector3.forward * kFrontPanelZStartOffset, new Vector3(0, kFrontPanelYOffset, kFrontPanelZEndOffset), m_LerpAmount);

			const float kHandleZOffset = 0.1f;
			m_HandleZOffset = kHandleZOffset * Mathf.Clamp01(m_LerpAmount * kAdditionalFrontPanelLerpPadding);

			UpdateHandles();

			// change blendshapes according to workspace rotation angle
			m_Frame.SetBlendShapeWeight(k_AngledFaceBlendShapeIndex, angledAmount * kLerpPadding);
			m_Frame.SetBlendShapeWeight(kRevealCompensationBlendShapeIndex, midRevealCorrectiveShapeAmount);
		}

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_TopFaceMaterial);
			ObjectUtils.Destroy(m_FrontFaceMaterial);
		}

		public void CloseClick()
		{
			if (closeClicked != null)
				closeClicked();
		}

		public void ResetSizeClick()
		{
			if (resetSizeClicked != null)
				resetSizeClicked();
		}

		IEnumerator IncreaseFrameThickness()
		{
			const float kTargetBlendAmount = 0f;
			const float kTargetDuration = 0.5f;
			var currentDuration = 0f;
			var currentBlendAmount = m_Frame.GetBlendShapeWeight(k_ThinFrameBlendShapeIndex);
			var currentVelocity = 0f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentBlendAmount = MathUtilsExt.SmoothDamp(currentBlendAmount, kTargetBlendAmount, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_Frame.SetBlendShapeWeight(k_ThinFrameBlendShapeIndex, currentBlendAmount);
				yield return null;
			}

			m_FrameThicknessCoroutine = null;
		}

		IEnumerator ResetFrameThickness()
		{
			const float kTargetBlendAmount = 50f;
			const float kTargetDuration = 0.5f;
			var currentDuration = 0f;
			var currentBlendAmount = m_Frame.GetBlendShapeWeight(k_ThinFrameBlendShapeIndex);
			var currentVelocity = 0f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentBlendAmount = MathUtilsExt.SmoothDamp(currentBlendAmount, kTargetBlendAmount, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_Frame.SetBlendShapeWeight(k_ThinFrameBlendShapeIndex, currentBlendAmount);
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
				currentAlpha = MathUtilsExt.SmoothDamp(currentAlpha, kTargetAlpha, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
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
				currentAlpha = MathUtilsExt.SmoothDamp(currentAlpha, kTargetAlpha, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_TopFaceMaterial.SetFloat(kMaterialHighlightAlphaProperty, currentAlpha);
				yield return null;
			}

			m_TopFaceVisibleCoroutine = null;
		}
	}
}
#endif
