#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public sealed class PinnedToolButton : MonoBehaviour, IPinnedToolButton,  ITooltip, ITooltipPlacement, ISetTooltipVisibility, ISetCustomTooltipColor
	{
		static Color s_FrameOpaqueColor;
		static bool s_Hovered;

		const int k_ActiveToolOrderPosition = 1; // A active-tool button position used in this particular ToolButton implementation
		const float k_alternateLocalScaleMultiplier = 0.85f; //0.64376f meets outer bounds of the radial menu
		const string k_MaterialColorProperty = "_Color";
		const string k_MaterialAlphaProperty = "_Alpha";
		const string k_SelectionToolTipText = "Selection Tool (cannot be closed)";
		const string k_MainMenuTipText = "Main Menu";
		readonly Vector3 k_ToolButtonActivePosition = new Vector3(0f, 0f, -0.035f);
		readonly Vector3 k_SemiTransparentIconContainerScale = new Vector3(1.375f, 1.375f, 1f);

		public Type toolType
		{
			get
			{
				return m_ToolType;
			}

			set
			{
				m_ToolType = value;

				m_GradientButton.gameObject.SetActive(true);

				if (m_ToolType != null)
				{
					Debug.LogError("Setting up button type : " + m_ToolType.ToString());
					gradientPair = UnityBrandColorScheme.saturatedSessionGradient;
					if (isSelectionTool || isMainMenu)
					{
						//order = isMainMenu ? menuButtonOrderPosition : activeToolOrderPosition;
						tooltipText = isSelectionTool ? k_SelectionToolTipText : k_MainMenuTipText;
						secondaryButtonCollidersEnabled = false;
					}
					else
					{
						tooltipText = toolType.Name;

						// Tools other than select fetch a random gradientPair; also used by the device when revealed
						//gradientPair = UnityBrandColorScheme.GetRandomCuratedLightGradient();
					}

					isActiveTool = isActiveTool;
					m_GradientButton.visible = true;
					//m_IconMaterial.SetColor(k_MaterialColorProperty, s_SemiTransparentFrameColor);

					//var targetScale = moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_alternateLocalScaleMultiplier;
					//var targetPosition = moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition;
					//this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateInitialReveal(targetPosition, targetScale));
				}
				else
				{
					m_GradientButton.visible = false;
					gradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
				}
			}
		}

		public int order
		{
			get { return m_Order; }
			set
			{
				//if (m_Order == value)
					//return;

				m_Order = value; // Position of this button in relation to other pinned tool buttons

				//Debug.LogError(m_ToolType.ToString() + " : <color=red>SETTING BUTTON ORDER TO : </color>" + m_Order + " - current button count : " + visibileButtonCount);

				highlighted = false;
				//m_InactivePosition = s_ActivePosition * ++value; // Additional offset for the button when it is visible and inactive
				//activeTool = activeTool;
				//const float kSmoothingMax = 50f;
				//const int kSmoothingIncreaseFactor = 10;
				//var smoothingFactor = Mathf.Clamp(kSmoothingMax- m_Order * kSmoothingIncreaseFactor, 0f, kSmoothingMax);
				//m_SmoothMotion.SetPositionSmoothing(smoothingFactor);
				//m_SmoothMotion.SetRotationSmoothing(smoothingFactor);
				//this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition());
				//m_LeftPinnedToolActionButton.visible = false;
				//m_RightPinnedToolActionButton.visible = false;

				// We move in counter-clockwise direction
				// Account for the input & position phase offset, based on the number of actions, rotating the menu content to be bottom-centered
				//const float kMaxPinnedToolButtonCount = 16; // TODO: add max count support in selectTool/setupPinnedToolButtonsForDevice
				//const float kRotationSpacing = 360f / kMaxPinnedToolButtonCount; // dividend should be the count of pinned tool buttons showing at this time
				//var phaseOffset = kRotationSpacing * 0.5f - (activeButtonCount * 0.5f) * kRotationSpacing;
				//var newTargetRotation = Quaternion.AngleAxis(phaseOffset + kRotationSpacing * m_Order, Vector3.down);

				//var mainMenuAndActiveButtonCount = 2;
				//var aboluteMenuButtonCount = isMainMenu ? mainMenuAndActiveButtonCount : activeButtonCount; // madates a fixed position for the MainMenu button, next to the ActiveToolButton
				this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition(m_Order));

				if(m_Order == -1)
					this.HideTooltip(this);

				/*
				if (aboluteMenuButtonCount > mainMenuAndActiveButtonCount)
					this.RestartCoroutine(ref m_HighlightCoroutine, AnimateSemiTransparent(m_Order != k_ActiveToolOrderPosition));
				else
					m_FrameMaterial.SetColor(k_MaterialColorProperty, s_FrameOpaqueColor);
				*/
			}
		}

		/// <summary>
		/// GradientPair should be set with new random gradientPair each time a new Tool is associated with this Button
		/// This gradientPair is also used to highlight the input device when appropriate
		/// </summary>
		public GradientPair gradientPair
		{
			get { return m_GradientPair; }
			set
			{
				m_GradientPair = value;
				customToolTipHighlightColor = value;
			}
		}

		/// <summary>
		/// Type, that if not null, denotes that preview-mode is enabled
		/// This is enabled when highlighting a tool on the main menu
		/// </summary>
		public Type previewToolType
		{
			get { return m_previewToolType; }
			set
			{
				m_previewToolType = value;

				if (m_previewToolType != null) // Show the highlight if the preview type is valid; hide otherwise
				{
					var tempToolGo = ObjectUtils.AddComponent(m_previewToolType, gameObject);
					var tempTool = tempToolGo as ITool;
					if (tempTool != null)
					{
						var iMenuIcon = tempTool as IMenuIcon;
						if (iMenuIcon != null)
							previewIcon = iMenuIcon.icon;

						ObjectUtils.Destroy(tempToolGo);
					}

					// Show the grayscale highlight when previewing a tool on this button
					m_GradientButton.highlightGradientPair = UnityBrandColorScheme.saturatedSessionGradient; // UnityBrandColorScheme.grayscaleSessionGradient;

					if (!previewIcon)
						m_GradientButton.SetContent(GetTypeAbbreviation(m_previewToolType));

					//tooltipText = "Assign " + m_previewToolType.Name;
					//customToolTipHighlightColor = UnityBrandColorScheme.grayscaleSessionGradient;
					//this.ShowTooltip(this);
				}
				else
				{
					previewToolDescription = null; // Clear the preview tooltip
					isActiveTool = isActiveTool; // Set active tool back to pre-preview state
					icon = icon; // Gradient button will set its icon back to that representing the current tool, if one existed before previewing new tool type in this button
					m_GradientButton.highlightGradientPair = gradientPair;
					//customToolTipHighlightColor = gradientPair;
					//this.HideTooltip(this);
					//tooltipText = (isSelectionTool || isMainMenu) ? (isSelectionTool ? k_SelectionToolTipText : k_MainMenuTipText) : toolType.Name;
				}

				m_GradientButton.highlighted = m_previewToolType != null;
				//this.RestartCoroutine(ref m_HighlightCoroutine, AnimateSemiTransparent(m_Order != k_ActiveToolOrderPosition));
			}
		}

		public string previewToolDescription
		{
			get { return m_previewToolDescription; }
			set
			{
				if (value != null)
				{
					m_previewToolDescription = value;
					this.ShowTooltip(this);
				}
				else
				{
					m_previewToolDescription = null;
					toolTipVisible = false;
				}
			}
		}

		public string tooltipText
		{
			get
			{
				return tooltip != null ? tooltip.tooltipText : (previewToolType == null ? m_TooltipText : previewToolDescription);
			}

			set { m_TooltipText = value; }
		}

		[SerializeField]
		GradientButton m_GradientButton;

		[SerializeField]
		SmoothMotion m_SmoothMotion;

		//[SerializeField]
		//PinnedToolActionButton m_LeftPinnedToolActionButton;

		//[SerializeField]
		//PinnedToolActionButton m_RightPinnedToolActionButton;

		[SerializeField]
		Transform m_IconContainer; // TODO: eliminate the reference to the icon container, use only the primary UI content container for any transformation

		[SerializeField]
		Transform m_PrimaryUIContentContainer;

		[SerializeField]
		CanvasGroup m_IconContainerCanvasGroup;

		//[SerializeField]
		//Collider m_RootCollider;

		[SerializeField]
		SkinnedMeshRenderer m_FrameRenderer;

		[SerializeField]
		SkinnedMeshRenderer m_InsetMeshRenderer;

		[SerializeField]
		Collider[] m_PrimaryButtonColliders;

		[SerializeField]
		GradientButton m_SecondaryGradientButton;

		[SerializeField]
		CanvasGroup m_SecondaryButtonContainerCanvasGroup;

		[SerializeField]
		SkinnedMeshRenderer m_SecondaryInsetMeshRenderer;

		[SerializeField]
		SkinnedMeshRenderer m_SecondaryInsetMaskMeshRenderer;

		[SerializeField]
		Collider[] m_SecondaryButtonColliders; // disable for the main menu button & solitary active tool button

		[SerializeField]
		Transform m_TooltipTarget;

		[SerializeField]
		Transform m_TooltipSource;

		[SerializeField]
		Vector3 m_AlternateLocalPosition;

		[SerializeField]
		Transform m_Inset;

		[SerializeField]
		Transform m_InsetMask;

		[SerializeField]
		Image m_ButtonIcon;

		Coroutine m_PositionCoroutine;
		Coroutine m_VisibilityCoroutine;
		Coroutine m_HighlightCoroutine;
		Coroutine m_ActivatorMoveCoroutine;
		Coroutine m_HoverCheckCoroutine;
		Coroutine m_SecondaryButtonVisibilityCoroutine;

		string m_TooltipText;
		string m_previewToolDescription;
		bool m_Revealed;
		bool m_MoveToAlternatePosition;
		int m_Order = -1;
		Type m_previewToolType;
		Type m_ToolType;
		GradientPair m_GradientPair;
		Material m_FrameMaterial;
		Material m_InsetMaterial;
		Vector3 m_OriginalLocalPosition;
		Vector3 m_OriginalLocalScale;
		Material m_IconMaterial;
		Vector3 m_OriginalIconContainerLocalScale;
		Sprite m_Icon;
		Sprite m_PreviewIcon;
		bool m_Highlighted;
		bool m_ActiveTool;

		public Transform tooltipTarget { get { return m_TooltipTarget; } set { m_TooltipTarget = value; } }
		public Transform tooltipSource { get { return m_TooltipSource; } }
		public TextAlignment tooltipAlignment { get; private set; }
		public Transform rayOrigin { get; set; }
		public Node node { get; set; }
		public ITooltip tooltip { private get; set; } // Overrides text
		public Action<ITooltip> showTooltip { private get; set; }
		public Action<ITooltip> hideTooltip { private get; set; }
		public GradientPair customToolTipHighlightColor { get; set; }
		public bool isSelectionTool { get { return m_ToolType != null && m_ToolType == typeof(Tools.SelectionTool); } }
		public bool isMainMenu { get { return m_ToolType != null && m_ToolType == typeof(IMainMenu); } }
		public int activeButtonCount { get; set; }
		public int maxButtonCount { get; set; }
		public Transform menuOrigin { get; set; }

		public Action<Transform, Transform> OpenMenu { get; set; }
		public Action<Type> selectTool { get; set; }
		public Func<bool> closeButton { get; set; }
		public Action<Transform, int, bool> highlightSingleButton { get; set; }
		public Action<Transform> selectHighlightedButton { get; set; }
		public Vector3 toolButtonActivePosition { get { return k_ToolButtonActivePosition; } } // Shared active button offset from the alternate menu
		public Func<Type, int> visibileButtonCount { get; set; }
		public bool implementsSecondaryButton { get; set; }
		public Action destroy { get { return DestroyButton; } }
		public Action<IPinnedToolButton> showAllButtons { get; set; }
		public Action hoverExit { get; set; }

		public event Action hovered;

		public bool isActiveTool
		{
			get { return m_ActiveTool; }
			set
			{
				//if (m_ActiveTool == value)
					//return;

				m_ActiveTool = value;

				m_GradientButton.normalGradientPair = m_ActiveTool ? gradientPair : UnityBrandColorScheme.darkGrayscaleSessionGradient;
				m_GradientButton.highlightGradientPair = m_ActiveTool ? UnityBrandColorScheme.darkGrayscaleSessionGradient : gradientPair;

				//if (activeTool) // TODO REMOVE IF NOT NEEDED
					//m_GradientButton.invertHighlightScale = value;

				m_GradientButton.highlighted = true;
				m_GradientButton.highlighted = false;
			}
		}

		public bool highlighted
		{
			get { return m_Highlighted; }
			set
			{
				if (m_Highlighted == value)
					return;

				m_Highlighted = value;
				m_GradientButton.highlighted = m_Highlighted;

				if (!m_Highlighted)
					this.HideTooltip(this);
				else
					this.ShowTooltip(this);

				if (implementsSecondaryButton && (!isMainMenu || !isSelectionTool))
				{
					// This show/hide functionality utilized by spatial scrolling
					if (m_Highlighted)
						this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, ShowSecondaryButton());
					else
						this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, HideSecondaryButton());
				}
			}
		}

		public bool secondaryButtonHighlighted { get { return m_SecondaryGradientButton.highlighted; } }

		public bool toolTipVisible
		{
			set
			{
				if (!value)
					this.HideTooltip(this);
			}
		}

		bool primaryButtonCollidersEnabled
		{
			set
			{
				foreach (var collider in m_PrimaryButtonColliders)
				{
					collider.enabled = value;
				}
			}
		}

		bool secondaryButtonCollidersEnabled
		{
			set
			{
				foreach (var collider in m_SecondaryButtonColliders)
				{
					collider.enabled = value;
				}
			}
		}

		/*
		public bool revealed
		{
			get { return m_Revealed; }
			set
			{
				if (m_Revealed == value || !gameObject.activeSelf || activeButtonCount <= k_ActiveToolOrderPosition + 1)
					return;

				m_Revealed = value;

				if (isMainMenu)
				{
					m_FrameMaterial.SetColor(k_MaterialColorProperty, s_FrameOpaqueColor * (value ? 0f : 1f));
					m_GradientButton.visible = !value;
					primaryButtonCollidersEnabled = !m_Revealed;
					return;
				}
				else
				{
					primaryButtonCollidersEnabled = m_Revealed;
				}

				//Debug.LogError(Time.frameCount + " : <color=blue>Highlighting : </color>" + toolType);

				var newOrderPosition = order;
				var buttonCount = 2; // MainMenu + ActiveTool button count
				var targetRotation = Quaternion.identity;
				if (m_Revealed)
				{
					buttonCount = activeButtonCount - 1; // The MainMenu button will be hidden, subtract 1 from the activeButtonCount
					this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, HideSecondaryButton());
					newOrderPosition -= 1;
					order = order;
					secondaryButtonCollidersEnabled = true;
				}
				else
				{
					// Set all buttons back to the center
					// Tools with orders greater than that of the active tool should hide themseleves when the pinned tools arent being hovered
					newOrderPosition = isMainMenu ? 0 : k_ActiveToolOrderPosition;
					secondaryButtonCollidersEnabled = false;
					this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, HideSecondaryButton());
				}

				this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition(newOrderPosition));
				//this.RestartCoroutine(ref m_HighlightCoroutine, AnimateSemiTransparent(!value && order < 1));
			}

			//get { return m_Revealed; }
		}
		*/

		public Sprite icon
		{
			get { return m_Icon; }
			set
			{
				m_PreviewIcon = null; // clear any cached preview icons
				m_Icon = value;

				if (m_Icon)
					m_GradientButton.SetContent(m_Icon);
				else
					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType)); // Set backup tool abbreviation if no icon is set
			}
		}

		public Sprite previewIcon
		{
			get { return m_PreviewIcon; }
			set
			{
				m_PreviewIcon = value;
				m_GradientButton.SetContent(m_PreviewIcon);
			}
		}

		public bool moveToAlternatePosition
		{
			get { return m_MoveToAlternatePosition; }
			set
			{
				if (m_MoveToAlternatePosition == value)
					return;

				m_MoveToAlternatePosition = value;

				this.StopCoroutine(ref m_ActivatorMoveCoroutine);

				m_ActivatorMoveCoroutine = StartCoroutine(AnimateMoveActivatorButton(m_MoveToAlternatePosition));
			}
		}

		public Vector3 primaryUIContentContainerLocalScale { get { return m_PrimaryUIContentContainer.localScale; } set { m_PrimaryUIContentContainer.localScale = value; } }
		public float iconHighlightedLocalZOffset { set { m_GradientButton.iconHighlightedLocalZOffset = value; } }

		void Awake()
		{
			const float kSemiTransparentAlphaValue = 0.5f;
			m_OriginalLocalPosition = transform.localPosition;
			m_OriginalLocalScale = transform.localScale;
			m_FrameMaterial = MaterialUtils.GetMaterialClone(m_FrameRenderer);
			var frameMaterialColor = m_FrameMaterial.color;
			s_FrameOpaqueColor = new Color(frameMaterialColor.r, frameMaterialColor.g, frameMaterialColor.b, 1f);
			m_FrameMaterial.SetColor(k_MaterialColorProperty, s_FrameOpaqueColor);

			m_IconMaterial = MaterialUtils.GetMaterialClone(m_ButtonIcon);
			m_InsetMaterial = MaterialUtils.GetMaterialClone(m_InsetMeshRenderer);
			m_OriginalIconContainerLocalScale = m_IconContainer.localScale;
		}

		void Start()
		{
			//m_GradientButton.onClick += ButtonClicked; // TODO remove after action button refactor

			Debug.LogWarning("Hide (L+R) pinned tool action buttons if button is the main menu button Hide select action button if button is in the first position (next to menu button)");

			//transform.parent = alternateMenuOrigin;

			if (m_ToolType == null)
			{
				//transform.localPosition = m_InactivePosition;
				m_GradientButton.gameObject.SetActive(false);
			}
			else
			{
				//transform.localPosition = activePosition;
			}

			//var tooltipSourcePosition = new Vector3(node == Node.LeftHand ? -0.01267f : 0.01267f, tooltipSource.localPosition.y, tooltipSource.localPosition.z);
			//var tooltipXOffset = node == Node.LeftHand ? -0.15f : 0.15f;
			//tooltipSource.localPosition = tooltipSourcePosition;
			//tooltipAlignment = node == Node.LeftHand ? TextAlignment.Right : TextAlignment.Left;
			//m_TooltipTarget.localPosition = new Vector3(tooltipXOffset, tooltipSourcePosition.y, tooltipSourcePosition.z);

			//var tooltipSourcePosition = new Vector3(0f, tooltipSource.localPosition.y, tooltipSource.localPosition.z);
			//tooltipSource.localPosition = tooltipSourcePosition;
			tooltipAlignment = TextAlignment.Center;
			//m_TooltipTarget.localPosition = new Vector3(0, 0, -0.5f);

			const float kIncreasedContainerContentsSpeedMultiplier = 2.5f;
			m_GradientButton.hoverEnter += OnBackgroundHoverEnter; // Display the foreground button actions
			m_GradientButton.hoverExit += OnActionButtonHoverExit;
			m_GradientButton.click += OnBackgroundButtonClick;
			m_GradientButton.containerContentsAnimationSpeedMultiplier = kIncreasedContainerContentsSpeedMultiplier;

			m_FrameRenderer.SetBlendShapeWeight(1, 0f);
			m_SecondaryInsetMeshRenderer.SetBlendShapeWeight(0, 100f);
			m_SecondaryInsetMaskMeshRenderer.SetBlendShapeWeight(0, 100f);

			m_SecondaryGradientButton.hoverEnter += OnBackgroundHoverEnter; // Display the foreground button actions
			m_SecondaryGradientButton.hoverExit += OnActionButtonHoverExit;
			m_SecondaryGradientButton.click += OnSecondaryButtonClicked;
			m_SecondaryButtonContainerCanvasGroup.alpha = 0f;
			//m_LeftPinnedToolActionButton.clicked = ActionButtonClicked;
			//m_LeftPinnedToolActionButton.hoverEnter = HoverButton;
			//m_LeftPinnedToolActionButton.hoverExit = OnActionButtonHoverExit;
			//m_RightPinnedToolActionButton.clicked = ActionButtonClicked;
			//m_RightPinnedToolActionButton.hoverEnter = HoverButton;
			//m_RightPinnedToolActionButton.hoverExit = OnActionButtonHoverExit;

			// Assign the select action button to the side closest to the opposite hand, that allows the arrow to also point in the direction the
			//var leftHand = node == Node.LeftHand;
			//m_RightPinnedToolActionButton.buttonType = leftHand ? PinnedToolActionButton.ButtonType.SelectTool : PinnedToolActionButton.ButtonType.Close;
			//m_LeftPinnedToolActionButton.buttonType = leftHand ? PinnedToolActionButton.ButtonType.Close : PinnedToolActionButton.ButtonType.SelectTool;

			//m_RightPinnedToolActionButton.rotateIcon = leftHand ? false : true;
			//m_LeftPinnedToolActionButton.rotateIcon = leftHand ? false : true;

			//m_LeftPinnedToolActionButton.visible = false;
			//m_RightPinnedToolActionButton.visible = false;

			//m_LeftPinnedToolActionButton.mainButtonCollider = m_RootCollider;
			//m_RightPinnedToolActionButton.mainButtonCollider = m_RootCollider;

			//m_ButtonCollider.enabled = true;
			//m_GradientButton.click += OnClick;
			//m_GradientButton.gameObject.SetActive(false);
		}

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_InsetMaterial);
			ObjectUtils.Destroy(m_IconMaterial);
			ObjectUtils.Destroy(m_FrameMaterial);

			this.StopCoroutine(ref m_PositionCoroutine);
			this.StopCoroutine(ref m_VisibilityCoroutine);
			this.StopCoroutine(ref m_HighlightCoroutine);
			this.StopCoroutine(ref m_ActivatorMoveCoroutine);
			this.StopCoroutine(ref m_HoverCheckCoroutine);
			this.StopCoroutine(ref m_SecondaryButtonVisibilityCoroutine);
			
			Debug.LogError("Perform Pulse up in PinnedToolsMenu level");
			//this.Pulse(rayOrigin, 0.5f, 0.2f, true, true);
		}

		void DestroyButton()
		{
			this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHideAndDestroy());
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			//this.Pulse(rayOrigin, 0.005f, 0.2f);
			Debug.LogError("Perform Pulse up in PinnedToolsMenu level");
		}

		// Create periodic table-style names for types
		string GetTypeAbbreviation(Type type)
		{
			var abbreviation = new StringBuilder();
			foreach (var ch in type.Name.ToCharArray())
			{
				if (char.IsUpper(ch))
					abbreviation.Append(abbreviation.Length > 0 ? char.ToLower(ch) : ch);

				if (abbreviation.Length >= 2)
					break;
			}

			return abbreviation.ToString();
		}

		void OnBackgroundHoverEnter ()
		{
			//Debug.LogWarning("<color=green>Background button was hovered, now triggereing the foreground action button visuals</color>");
			//if (m_PositionCoroutine != null || m_SecondaryGradientButton.highlighted)
				//return;

			s_Hovered = true;

			//this.Pulse(rayOrigin, 0.005f, 0.175f);
			Debug.LogError("Perform Pulse up in PinnedToolsMenu level");

			if (isMainMenu)
			{
				m_GradientButton.highlighted = true;
				return;
			}
			else if (implementsSecondaryButton)
			{
				this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, ShowSecondaryButton());
			}

			if (hovered != null) // Raised in order to trigger the haptic in the PinnedToolsMenu
				hovered();

			//if (!m_LeftPinnedToolActionButton.revealed && !m_RightPinnedToolActionButton.revealed)
			//{
				//Debug.LogWarning("<color=green>Background button was hovered, now triggereing the foreground action button visuals</color>");
				//m_RootCollider.enabled = false;
				m_GradientButton.highlighted = true;
				//m_GradientButton.visible = false;

				//Debug.LogWarning("Handle for disabled buttons not being shown, ie the promotote(green) button on the first/selected tool");

			//revealAllToolButtons(rayOrigin, true);
			showAllButtons(this);
			//HoverButton();
			//m_ButtonCollider.enabled = false;
			
			//}
		}

		/*
		void HoverButton()
		{
			if (order < 2 && (isSelectionTool || isMainMenu)) // The main menu and the active tool occupy orders 0 and 1; don't show any action buttons for buttons in either position
			{
				//m_RightPinnedToolActionButton.visible = false;
				//m_LeftPinnedToolActionButton.visible = false;
				//m_RootCollider.enabled = true;
				//StartCoroutine(DelayedCollderEnable());
			}
			else if (isSelectionTool)
			{

				if (activeTool)
				{
					m_RightPinnedToolActionButton.visible = false;
					m_LeftPinnedToolActionButton.visible = false;
					StartCoroutine(DelayedCollderEnable());
				}
				else

				{
					//m_RightPinnedToolActionButton.visible = IsSelectToolButton(m_RightPinnedToolActionButton.buttonType) ? true : false;
					//m_LeftPinnedToolActionButton.visible = IsSelectToolButton(m_LeftPinnedToolActionButton.buttonType) ? true : false;
				}
			} else
			{
				// Hide the select action button if this tool button is already the selected tool, else show the close button for inactive tools
				//m_RightPinnedToolActionButton.visible = IsSelectToolButton(m_RightPinnedToolActionButton.buttonType) ? !activeTool : true;
				//m_LeftPinnedToolActionButton.visible = IsSelectToolButton(m_LeftPinnedToolActionButton.buttonType) ? !activeTool : true;
			}

			revealAllToolButtons(rayOrigin, true);
		}
	*/

		void OnActionButtonHoverExit()
		{
			ActionButtonHoverExit();
		}

		void ActionButtonHoverExit(bool waitBeforeClosingAllButtons = true)
		{
			if (m_PositionCoroutine != null)
				return;

			if (isMainMenu)
			{
				m_GradientButton.highlighted = false;
				return;
			}

			//this.RestartCoroutine(ref m_HoverCheckCoroutine, DelayedHoverExitCheck(waitBeforeClosingAllButtons));

			if (!m_SecondaryGradientButton.highlighted)
				this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, HideSecondaryButton());

			hoverExit();
		}

		/*
		void ActionButtonClicked(PinnedToolActionButton button)
		{
			Debug.LogError("Action Button selectTool!");
			if (order > menuButtonOrderPosition)
			{
				// TODO: SELECT ACTION BUTTONS should be able to be interacted with due to their being hidden, so no need to handle for that case
				// Buttons in the activeToolOrderPosition cannot be selected when selectTool
				if (button.buttonType == PinnedToolActionButton.ButtonType.SelectTool && order > activeToolOrderPosition)
				{
					selectTool(rayOrigin, m_ToolType); // ButtonClicked will set button order to 0
					activeTool = activeTool;
					//SetButtonGradients(this.ButtonClicked(rayOrigin, m_ToolType));
				}
				else // Handle action buttons assigned Close-Tool functionality
				{
					//if (!isSelectionTool)
						this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHideAndDestroy());
					//else
						//Debug.LogError("<color=red>CANNOT DELETE THE SELECT TOOL!!!!!</color>");

					deletePinnedToolButton(rayOrigin, this);
				}

				OnActionButtonHoverExit(false);
				//m_LeftPinnedToolActionButton.revealed = false;
				//m_RightPinnedToolActionButton.revealed = false;
			}
		}
		*/

		void OnBackgroundButtonClick()
		{
			Debug.LogWarning("<color=orange>OnBackgroundButtonClick : </color>" + name + " : " + toolType);
			// in this case display the hover state for the gradient button, then enable visibility for each of the action buttons

			// Hide both action buttons if the user is no longer hovering over the button
			//if (!m_LeftPinnedToolActionButton.revealed && !m_RightPinnedToolActionButton.revealed)
			//{
			//}

			selectTool(toolType); // Perform clik for a ToolButton that doesn't utilize ToolActionButtons

			if (!isMainMenu)
			{
				ActionButtonHoverExit(false);
				//revealAllToolButtons(rayOrigin, true);
			}

			m_GradientButton.UpdateMaterialColors();
		}

		void OnSecondaryButtonClicked()
		{
			this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHideAndDestroy());
			closeButton();
			//this.Pulse(rayOrigin, 0.5f, 0.1f, true, true);
			Debug.LogError("Perform Pulse up in PinnedToolsMenu level");
			//deleteHighlightedButton(rayOrigin);
			//deletePinnedToolButton(rayOrigin, this);
			ActionButtonHoverExit(false);
		}

		IEnumerator AnimateInitialReveal(Vector3 targetPosition, Vector3 targetScale)
		{
			m_IconContainerCanvasGroup.alpha = 1f;
			const int kDurationShapeAmount = 4;
			const float kTimeScalar = 3f;
			const float kAdditionalIconContainerScaleSpeed = 2f;
			var duration = 0f;
			while (duration < 2)
			{
				duration += Time.unscaledDeltaTime * kTimeScalar;
				var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), kDurationShapeAmount);
				m_IconContainer.localScale = Vector3.Lerp(Vector3.zero, k_SemiTransparentIconContainerScale, durationShaped * kAdditionalIconContainerScaleSpeed);
				transform.localPosition = Vector3.Lerp(Vector3.zero, targetPosition, durationShaped);
				transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, durationShaped);
				yield return null;
			}

			m_IconContainer.localScale = k_SemiTransparentIconContainerScale;
			transform.localPosition = targetPosition;
			transform.localScale = targetScale;
			m_VisibilityCoroutine = null;
		}

		IEnumerator AnimateHideAndDestroy()
		{
			this.StopCoroutine(ref m_PositionCoroutine);
			this.StopCoroutine(ref m_HighlightCoroutine);
			this.StopCoroutine(ref m_ActivatorMoveCoroutine);
			this.StopCoroutine(ref m_HoverCheckCoroutine);
			this.StopCoroutine(ref m_SecondaryButtonVisibilityCoroutine);

			this.HideTooltip(this);
			var duration = 0f;
			var currentScale = transform.localScale;
			var targetScale = Vector3.zero;
			while (duration < 1)
			{
				duration += Time.unscaledDeltaTime * 3f;
				var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), 4);
				transform.localScale = Vector3.Lerp(currentScale, targetScale, durationShaped);
				yield return null;
			}

			transform.localScale = targetScale;
			m_VisibilityCoroutine = null;
			ObjectUtils.Destroy(gameObject, 0.1f);
		}

		IEnumerator AnimateHide()
		{
			//primaryButtonCollidersEnabled = false;
			//secondaryButtonCollidersEnabled = false;
			const float kTimeScalar = 8f;
			var targetPosition = Vector3.zero;
			var currentPosition = transform.localPosition;

			var currentFrameColor = m_FrameMaterial.color;
			var targetFrameColor = Color.clear;
			var targetIconColor = Color.clear;
			//var currentInsetScale = m_Inset.localScale;
			//var targetInsetScale = makeSemiTransparent ? new Vector3(1f, 0f, 1f) : Vector3.one;
			//var currentInsetMaskScale = m_InsetMask.localScale;
			//var targetInsetMaskScale = makeSemiTransparent ? Vector3.one * 1.45f : Vector3.one;
			var currentIconScale = m_IconContainer.localScale;
			var targetIconContainerScale = Vector3.zero;
			var transitionAmount = 0f;
			var currentScale = transform.localScale;
			while (transitionAmount < 1)
			{
				transitionAmount += Time.unscaledDeltaTime * kTimeScalar;
				var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
				/*
				m_FrameMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentFrameColor, targetFrameColor, shapedAmount));
				//m_Inset.localScale = Vector3.Lerp(currentInsetScale, targetInsetScale, shapedAmount);
				//m_InsetMask.localScale = Vector3.Lerp(currentInsetMaskScale, targetInsetMaskScale, Mathf.Pow(shapedAmount, 3));
				m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, Mathf.Lerp(currentInsetAlpha, targetInsetAlpha, shapedAmount));
				m_IconMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentIconColor, targetIconColor, shapedAmount * 2));
				//var shapedTransitionAmount = Mathf.Pow(transitionAmount, makeSemiTransparent ? 2 : 1) * kFasterMotionMultiplier;
				*/
				//CorrectIconRotation();
				m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconContainerScale, shapedAmount);
				transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
				transform.localScale = Vector3.Lerp(currentScale, Vector3.zero, shapedAmount);
				yield return null;
			}

			/*
			m_FrameMaterial.SetColor(k_MaterialColorProperty, targetFrameColor);
			m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, targetInsetAlpha);
			//m_Inset.localScale = targetInsetScale;
			//m_InsetMask.localScale = targetInsetMaskScale;
			m_IconMaterial.SetColor(k_MaterialColorProperty, targetIconColor);
			*/
			m_IconContainer.localScale = targetIconContainerScale;
			transform.localPosition = targetPosition;
			m_VisibilityCoroutine = null;
		}

		IEnumerator AnimateShow()
		{
			//primaryButtonCollidersEnabled = false;
			//secondaryButtonCollidersEnabled = false;

			const float kTimeScalar = 8f;
			var targetScale = moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_alternateLocalScaleMultiplier;
			var targetPosition = moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition;

			var currentFrameColor = m_FrameMaterial.color;
			var targetFrameColor = Color.clear;
			var targetIconColor = Color.clear;
			//var currentInsetScale = m_Inset.localScale;
			//var targetInsetScale = makeSemiTransparent ? new Vector3(1f, 0f, 1f) : Vector3.one;
			//var currentInsetMaskScale = m_InsetMask.localScale;
			//var targetInsetMaskScale = makeSemiTransparent ? Vector3.one * 1.45f : Vector3.one;
			var currentIconScale = m_IconContainer.localScale;
			var targetIconContainerScale = m_OriginalIconContainerLocalScale;
			var transitionAmount = 0f;
			var currentScale = transform.localScale;
			var currentPosition = transform.localPosition;
			while (transitionAmount < 1)
			{
				transitionAmount += Time.unscaledDeltaTime * kTimeScalar;
				var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
				/*
				m_FrameMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentFrameColor, targetFrameColor, shapedAmount));
				//m_Inset.localScale = Vector3.Lerp(currentInsetScale, targetInsetScale, shapedAmount);
				//m_InsetMask.localScale = Vector3.Lerp(currentInsetMaskScale, targetInsetMaskScale, Mathf.Pow(shapedAmount, 3));
				m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, Mathf.Lerp(currentInsetAlpha, targetInsetAlpha, shapedAmount));
				m_IconMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentIconColor, targetIconColor, shapedAmount * 2));
				//var shapedTransitionAmount = Mathf.Pow(transitionAmount, makeSemiTransparent ? 2 : 1) * kFasterMotionMultiplier;
				transform.localPosition = Vector3.Lerp(Vector3.zero, targetPosition, durationShaped);
				*/
				//CorrectIconRotation();
				m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconContainerScale, shapedAmount);
				transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
				transform.localScale = Vector3.Lerp(currentScale, targetScale, shapedAmount);
				yield return null;
			}

			/*
			m_FrameMaterial.SetColor(k_MaterialColorProperty, targetFrameColor);
			m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, targetInsetAlpha);
			//m_Inset.localScale = targetInsetScale;
			//m_InsetMask.localScale = targetInsetMaskScale;
			m_IconMaterial.SetColor(k_MaterialColorProperty, targetIconColor);
			*/
			transform.localPosition = targetPosition;
			transform.localScale = targetScale;
			m_IconContainer.localScale = targetIconContainerScale;
			m_VisibilityCoroutine = null;
		}

		//IEnumerator AnimatePosition(int orderPosition, int buttonCount)
		IEnumerator AnimatePosition(int orderPosition)
		{
			//Debug.LogError(m_ToolType + " - Animate Button position : " + orderPosition + " - BUTTON COUNT: " + visibileButtonCount);
			primaryButtonCollidersEnabled = false;
			secondaryButtonCollidersEnabled = false;
			//if (!activeTool) // hide a button if it is not the active tool button and the buttons are no longer revealed
				//gameObject.SetActive(true);

			//if (order != activeToolOrderPosition)
				//m_RootCollider.enabled = false;

			this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, HideSecondaryButton());

			if (orderPosition == -1)
				this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHide());
			else
				this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());

			const float kTimeScalar = 6f;
			const float kCenterLocationAmount = 0.5f;
			const float kCircularRange = 360f;
			const int kDurationShapeAmount = 3;
			var rotationSpacing = kCircularRange / maxButtonCount; // dividend should be the count of pinned tool buttons showing at this time
			var phaseOffset = orderPosition > -1 ? rotationSpacing * kCenterLocationAmount - (visibileButtonCount(m_ToolType) * kCenterLocationAmount) * rotationSpacing : 0; // Center the MainMenu & Active tool buttons at the bottom of the RadialMenu
			var targetRotation = orderPosition > -1 ? Quaternion.AngleAxis(phaseOffset + rotationSpacing * Mathf.Max(0f, orderPosition), Vector3.down) : Quaternion.identity;

			var duration = 0f;
			//var currentPosition = transform.localPosition;
			//var targetPosition = activeTool ? activePosition : m_InactivePosition;
			var currentCanvasAlpha = m_IconContainerCanvasGroup.alpha;
			var targetCanvasAlpha = orderPosition > -1 ? 1f : 0f;
			var currentRotation = transform.localRotation;
			var positionWait = 1f;// (order + 5) * 0.1f;
			while (duration < 1)
			{
				duration += Time.unscaledDeltaTime * kTimeScalar * positionWait;
				var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), kDurationShapeAmount);
				transform.localRotation = Quaternion.Lerp(currentRotation, targetRotation, durationShaped);
				m_IconContainerCanvasGroup.alpha = Mathf.Lerp(currentCanvasAlpha, targetCanvasAlpha, durationShaped);
				CorrectIconRotation();
				//transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, durationShaped);
				yield return null;
			}

			//if (!m_Revealed && orderPosition == 0 && buttonCount == 1 && !activeTool) // hide a button if it is not the active tool button and the buttons are no longer revealed
				//gameObject.SetActive(false);

			//transform.localPosition = targetPosition;
			transform.localRotation = targetRotation;
			CorrectIconRotation();
			primaryButtonCollidersEnabled = orderPosition > -1 ? true : false;
			secondaryButtonCollidersEnabled = orderPosition > -1 ? true : false;
			m_PositionCoroutine = null;

			if (implementsSecondaryButton && orderPosition > -1 && m_GradientButton.highlighted)
				this.RestartCoroutine(ref m_SecondaryButtonVisibilityCoroutine, ShowSecondaryButton());
		}

		/*
		IEnumerator AnimateSemiTransparentX(bool makeSemiTransparent)
		{
			//if (!makeSemiTransparent)
				//yield return new WaitForSeconds(1f); // Pause before making opaque

			if (makeSemiTransparent && activeTool)
				yield break;

			//Debug.LogWarning("<color=blue>AnimateSemiTransparent : </color>" + makeSemiTransparent);
			//const float kFasterMotionMultiplier = 2f;
			var transitionAmount = 0f;
			//var positionWait = (order + 1) * 0.25f; // pad the order index for a faster start to the transition
			//var semiTransparentTargetScale = new Vector3(0.9f, 0.15f, 0.9f);
			//var transparentFrameColor = new Color (s_FrameOpaqueColor.r, s_FrameOpaqueColor.g, s_FrameOpaqueColor.b, 0f);
			var currentFrameColor = m_FrameMaterial.color;
			var targetFrameColor = makeSemiTransparent ? s_SemiTransparentFrameColor : s_FrameOpaqueColor;
			var currentInsetAlpha = m_InsetMaterial.GetFloat(k_MaterialAlphaProperty);
			var targetInsetAlpha = makeSemiTransparent ? 0.25f : 1f;
			var currentIconColor = m_IconMaterial.GetColor(k_MaterialColorProperty);
			var targetIconColor = makeSemiTransparent ? s_SemiTransparentFrameColor : Color.white;
			var currentInsetScale = m_Inset.localScale;
			var targetInsetScale = makeSemiTransparent ? new Vector3(1f, 0f, 1f) : Vector3.one;
			var currentInsetMaskScale = m_InsetMask.localScale;
			var targetInsetMaskScale = makeSemiTransparent ? Vector3.one * 1.45f : Vector3.one;
			var currentIconScale = m_IconContainer.localScale;
			var targetIconContainerScale = makeSemiTransparent ? k_SemiTransparentIconContainerScale : Vector3.one;
			var speedMultiplier = makeSemiTransparent ? 4f : 6.5f; // Slower transparent fade; faster opaque fade
			while (transitionAmount < 1)
			{
				transitionAmount += Time.unscaledDeltaTime * speedMultiplier;
				var shapedAmount = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(transitionAmount), 3);
				m_FrameMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentFrameColor, targetFrameColor, shapedAmount));
				m_Inset.localScale = Vector3.Lerp(currentInsetScale, targetInsetScale, shapedAmount);
				m_InsetMask.localScale = Vector3.Lerp(currentInsetMaskScale, targetInsetMaskScale, Mathf.Pow(shapedAmount, 3));
				m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, Mathf.Lerp(currentInsetAlpha, targetInsetAlpha, shapedAmount));
				m_IconMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentIconColor, targetIconColor, shapedAmount * 2));
				//var shapedTransitionAmount = Mathf.Pow(transitionAmount, makeSemiTransparent ? 2 : 1) * kFasterMotionMultiplier;
				m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconContainerScale, shapedAmount);
				CorrectIconRotation();
				yield return null;
			}

			m_FrameMaterial.SetColor(k_MaterialColorProperty, targetFrameColor);
			m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, targetInsetAlpha);
			m_Inset.localScale = targetInsetScale;
			m_InsetMask.localScale = targetInsetMaskScale;
			m_IconMaterial.SetColor(k_MaterialColorProperty, targetIconColor);
			m_IconContainer.localScale = targetIconContainerScale;
		}
		*/

		IEnumerator AnimateMoveActivatorButton(bool moveToAlternatePosition = true)
		{
			const float kSpeedDecreaseScalar = 0.275f;
			var amount = 0f;
			var currentPosition = transform.localPosition;
			var targetPosition = moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition;
			var currentLocalScale = transform.localScale;
			var targetLocalScale = moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_alternateLocalScaleMultiplier;
			var speed = moveToAlternatePosition ? 5f : 4.5f; // perform faster is returning to original position
			speed += (order + 1) * kSpeedDecreaseScalar;
			while (amount < 1f)
			{
				amount += Time.unscaledDeltaTime * speed;
				var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(amount);
				transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
				transform.localScale = Vector3.Lerp(currentLocalScale, targetLocalScale, shapedAmount);
				yield return null;
			}

			transform.localPosition = targetPosition;
			transform.localScale = targetLocalScale;
			m_ActivatorMoveCoroutine = null;
		}

		IEnumerator DelayedHoverExitCheck(bool waitBeforeClosingAllButtons = true)
		{
			s_Hovered = false;

			if (waitBeforeClosingAllButtons)
			{
				var duration = Time.unscaledDeltaTime;
				while (duration < 0.25f)
				{
					duration += Time.unscaledDeltaTime;
					yield return null;

					if ((s_Hovered || m_PositionCoroutine != null) || m_SecondaryGradientButton.highlighted)
					{
						m_HoverCheckCoroutine = null;
						yield break;
					}
				}
			}

			// Only proceed if no other button is being hovered
			m_GradientButton.highlighted = false;
			hoverExit();
			m_GradientButton.UpdateMaterialColors();
			m_HoverCheckCoroutine = null;
		}

		void CorrectIconRotation()
		{
			const float kIconLookForwardOffset = 0.5f;
			var iconLookDirection = m_IconContainer.transform.position + transform.forward * kIconLookForwardOffset; // set a position offset above the icon, regardless of the icon's rotation
			m_IconContainer.LookAt(iconLookDirection);
			m_IconContainer.localEulerAngles = new Vector3(0f, 0f, m_IconContainer.localEulerAngles.z);

			//tooltipSource.localRotation = m_IconContainer.localRotation;
			//var angle = m_IconContainer.localEulerAngles.z;
			//m_TooltipTarget.localEulerAngles = new Vector3(0, 0, angle);

			//var yaw = transform.localRotation.eulerAngles.y;
			//tooltipAlignment = yaw > 90 && yaw <= 270 ? TextAlignment.Center : TextAlignment.Center;
		}

		IEnumerator ShowSecondaryButton()
		{
			//Debug.LogError("<color=black>Waiting before SHOWING SECONDARY BUTTON</color>");

			// Don't perform additional animated visuals if already in a fully revealed state
			if (Mathf.Approximately(m_SecondaryButtonContainerCanvasGroup.alpha, 1f))
			{
				m_SecondaryButtonVisibilityCoroutine = null;
				yield break;
			}

			const float kSecondaryButtonFrameVisibleBlendShapeWeight = 16f; // The extra amount of the frame to show on hover-before the full reveal of the secondary button
			const float kTargetDuration = 1f;
			const int kIntroDurationMultiplier = 10;
			var currentVisibilityAmount = m_FrameRenderer.GetBlendShapeWeight(1);
			var currentDuration = 0f;
			while (currentDuration < kTargetDuration)
			{
				var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentDuration += Time.unscaledDeltaTime * kIntroDurationMultiplier);
				m_FrameRenderer.SetBlendShapeWeight(1, Mathf.Lerp(currentVisibilityAmount, kSecondaryButtonFrameVisibleBlendShapeWeight, shapedAmount));
				yield return null;
			}

			const float kDelayBeforeSecondaryButtonReveal = 0.25f;
			currentDuration = 0f;
			while (currentDuration < kDelayBeforeSecondaryButtonReveal)
			{
				currentDuration += Time.unscaledDeltaTime;
				yield return null;
			}

			const float kFrameSecondaryButtonVisibleBlendShapeWeight = 61f;
			const float kSecondaryButtonVisibleBlendShapeWeight = 46f;
			const int kDurationMultiplier = 25;

			this.StopCoroutine(ref m_HighlightCoroutine);

			var currentSecondaryButtonVisibilityAmount = m_SecondaryInsetMeshRenderer.GetBlendShapeWeight(0);
			var currentSecondaryCanvasGroupAlpha = m_SecondaryButtonContainerCanvasGroup.alpha;
			currentVisibilityAmount = m_FrameRenderer.GetBlendShapeWeight(1);
			currentDuration = 0f;
			while (currentDuration < 1f)
			{
				currentDuration += Time.unscaledDeltaTime * kDurationMultiplier;
				var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentDuration);
				m_FrameRenderer.SetBlendShapeWeight(1, Mathf.Lerp(currentVisibilityAmount, kFrameSecondaryButtonVisibleBlendShapeWeight, shapedAmount));
				m_SecondaryInsetMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(currentSecondaryButtonVisibilityAmount, kSecondaryButtonVisibleBlendShapeWeight, shapedAmount));
				m_SecondaryInsetMaskMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(currentSecondaryButtonVisibilityAmount, kSecondaryButtonVisibleBlendShapeWeight, shapedAmount));
				m_SecondaryButtonContainerCanvasGroup.alpha = Mathf.Lerp(currentSecondaryCanvasGroupAlpha, 1f, shapedAmount);
				yield return null;
			}

			m_SecondaryButtonVisibilityCoroutine = null;
		}

		IEnumerator HideSecondaryButton()
		{
			const float kMaxDelayDuration = 0.125f;
			var delayDuration = 0f;
			while (delayDuration < kMaxDelayDuration)
			{
				delayDuration += Time.unscaledDeltaTime;
				yield return null;
			}

			const float kSecondaryButtonHiddenBlendShapeWeight = 100f;
			const int kDurationMultiplier = 12;
			var currentVisibilityAmount = m_FrameRenderer.GetBlendShapeWeight(1);
			var currentSecondaryButtonVisibilityAmount = m_SecondaryInsetMeshRenderer.GetBlendShapeWeight(0);
			var currentSecondaryCanvasGroupAlpha = m_SecondaryButtonContainerCanvasGroup.alpha;
			var amount = 0f;
			while (amount < 1f)
			{
				yield return null;

				if (m_SecondaryGradientButton.highlighted)
				{
					m_SecondaryButtonVisibilityCoroutine = null;
					yield break;
				}

				this.StopCoroutine(ref m_HighlightCoroutine);

				amount += Time.unscaledDeltaTime * kDurationMultiplier;
				var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(amount);
				m_FrameRenderer.SetBlendShapeWeight(1, Mathf.Lerp(currentVisibilityAmount, 0f, shapedAmount));
				m_SecondaryInsetMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(currentSecondaryButtonVisibilityAmount, kSecondaryButtonHiddenBlendShapeWeight, shapedAmount));
				m_SecondaryInsetMaskMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(currentSecondaryButtonVisibilityAmount, kSecondaryButtonHiddenBlendShapeWeight, shapedAmount));
				m_SecondaryButtonContainerCanvasGroup.alpha = Mathf.Lerp(currentSecondaryCanvasGroupAlpha, 0f, shapedAmount);
			}

			m_SecondaryButtonVisibilityCoroutine = null;
		}
	}
}
#endif
