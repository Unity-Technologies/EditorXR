using System;
using UnityEngine.VR.Handles;
using UnityEngine.UI;

namespace UnityEngine.VR.Workspaces
{
	public class WorkspaceUI : MonoBehaviour
	{
		public event Action closeClicked = delegate { };
		public event Action lockClicked = delegate { };

		private float m_OriginalUIContainerLocalYPos;
		Material m_FrameGradientMaterial;

		private const float kPanelOffset = -0.09f; // The panel needs to be pulled back slightly

		[SerializeField]
		private RectTransform m_UIContentContainer;

		[SerializeField]
		private Image m_FrontLeftResizeIcon;

		[SerializeField]
		private Image m_FrontRightResizeIcon;

		[SerializeField]
		private Image m_BackLeftResizeIcon;

		[SerializeField]
		private Image m_BackRightResizeIcon;

		[SerializeField]
		private Image m_LeftSideFrontResizeIcon;

		[SerializeField]
		private Image m_LeftSideBackResizeIcon;

		[SerializeField]
		private Image m_RightSideFrontResizeIcon;

		[SerializeField]
		private Image m_RightSideBackResizeIcon;

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
		
		private const string kBottomGradientProperty = "_ColorBottom";
		private const string kTopGradientProperty = "_ColorTop";
		private const int kAngledFaceBlendShapeIndex = 2;
		private const int kHiddenFacesBlendShapeIndex = 3;

		public bool dynamicFaceAdjustment { get; set; }

		public bool workspaceBaseInteractive
		{
			get { return m_workspaceBaseInteractive; }
			set
			{
				m_workspaceBaseInteractive = value;
				dynamicFaceAdjustment = false;

				if (m_workspaceBaseInteractive == false)
					m_Frame.SetBlendShapeWeight(kHiddenFacesBlendShapeIndex, 100f);
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
				float handleScale = leftHandle.transform.localScale.z;

				m_LeftHandle.transform.localPosition = new Vector3(-m_Bounds.extents.x + handleScale * 0.5f, m_LeftHandle.transform.localPosition.y, 0);
				m_LeftHandle.transform.localScale = new Vector3(m_Bounds.size.z, handleScale, handleScale);

				m_FrontHandle.transform.localPosition = new Vector3(0, m_FrontHandle.transform.localPosition.y, -m_Bounds.extents.z - handleScale);
				m_FrontHandle.transform.localScale = new Vector3(m_Bounds.size.x, handleScale, handleScale);

				m_RightHandle.transform.localPosition = new Vector3(m_Bounds.extents.x - handleScale * 0.5f, m_RightHandle.transform.localPosition.y, 0);
				m_RightHandle.transform.localScale = new Vector3(m_Bounds.size.z, handleScale, handleScale);

				m_BackHandle.transform.localPosition = new Vector3(0, m_BackHandle.transform.localPosition.y, m_Bounds.extents.z - handleScale);
				m_BackHandle.transform.localScale = new Vector3(m_Bounds.size.x, handleScale, handleScale);

				// Resize content container
				m_UIContentContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_Bounds.size.x);
				m_UIContentContainer.localPosition = new Vector3(0, m_OriginalUIContainerLocalYPos, -m_Bounds.extents.z);

				// Resize front panel
				if (dynamicFaceAdjustment == false)
					m_FrontPanel.localPosition = new Vector3(0f, m_OriginalFontPanelLocalPosition.y, kPanelOffset);

				//m_FrameFrontFaceTransform.localScale = new Vector3(m_Bounds.size.x * m_FaceWidthMultiplier, 1f, 1f);

				m_GrabCollider.size = new Vector3(m_Bounds.size.x, m_GrabCollider.size.y, m_GrabCollider.size.z);
			}
		}
		private Bounds m_Bounds;

		private void ResizeHighlightBegin(BaseHandle baseHandle, HandleEventData eventData)
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

		private void ResizeHighlightEnd(BaseHandle baseHandle, HandleEventData eventData)
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

		private void Awake()
		{
			m_OriginalUIContainerLocalYPos = m_UIContentContainer.localPosition.y;
			m_OriginalFontPanelLocalPosition = m_FrontPanel.localPosition;

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

			/*
			m_Frame.sharedMaterials = U.Material.GetMaterialClones(m_Frame); // no need to assign again, as clones are assigned therein

			foreach (var material in m_Frame.sharedMaterials)
			{
				Debug.LogError(material.name);
				if (material.name == "GradientBorder")
				{
					m_FrameGradientMaterial = material;
					break;
				}
			}

			var gradientPair = UnityBrandColorScheme.GetRandomGradient();
			m_FrameGradientMaterial.SetColor(kTopGradientProperty, gradientPair.a);
			m_FrameGradientMaterial.SetColor(kBottomGradientProperty, gradientPair.b);
			*/

			//m_Frame.SetBlendShapeWeight(kAngledFaceBlendShapeIndex, Random.Range(0, 100f));
		}

		private float m_AngledAmount; 
		private Vector3 m_BaseFrontPanelRotation = Vector3.zero;
		private Vector3 m_MaxFrontPanelRotation = new Vector3(45f, 0f, 0f);
		private float kMaxAlternateFrontPanelLocalZOffset = -0.075f;//-0.1f; //-0.0575f;// -0.3009003f;
		private float kMaxAlternateFrontPanelLocalYOffset = -0.005f;//-0.03813409f;
		private Vector3 m_OriginalFontPanelLocalPosition;
		private float kMaxBlendShapeAngle = 90f;

		private void Update()
		{
			m_FrameFrontFaceTransform.localScale = new Vector3(m_Bounds.size.x * m_FaceWidthMatchMultiplier, 1f, 1f); // hack remove

			if (dynamicFaceAdjustment == false)
				return;

			// sin of x rotation drives the blendeshape value
			//Debug.LogWarning("<color=green>" + Mathf.Sin(transform.rotation.eulerAngles.x) + "</color> : " + transform.rotation.eulerAngles.x);
			//Debug.LogWarning("<color=green>" + Mathf.Deg2Rad * transform.rotation.eulerAngles.x + "</color> : " + transform.rotation.eulerAngles.x);

			m_AngledAmount = Mathf.Clamp(Mathf.DeltaAngle(transform.rotation.eulerAngles.x, 0f), 0f, 100f);

			//Debug.LogWarning("<color=purple>" + m_AngledAmount + "</color> : " + transform.rotation.eulerAngles.x);

			float lerpAmount = m_AngledAmount / 90f;
			m_FrontPanel.localRotation = Quaternion.Euler(Vector3.Lerp(m_BaseFrontPanelRotation, m_MaxFrontPanelRotation, lerpAmount));  // qua Quaternion.Euler(Mathf.Lerp(0, 45, angledBlendshapeAmount), 0f, 0f);
			m_FrontPanel.localPosition = new Vector3(0f, Mathf.Lerp(m_OriginalFontPanelLocalPosition.y, kMaxAlternateFrontPanelLocalYOffset, lerpAmount), Mathf.Lerp(kPanelOffset, kMaxAlternateFrontPanelLocalZOffset, lerpAmount));

			m_Frame.SetBlendShapeWeight(kAngledFaceBlendShapeIndex, m_AngledAmount);

			//Debug.LogWarning("<color=yellow>" + m_AngledAmount + "</color>");
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