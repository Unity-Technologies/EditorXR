#if UNITY_EDITOR
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public sealed class PinnedToolButton : MonoBehaviour, ISelectTool, ITooltip, ITooltipPlacement, ISetTooltipVisibility, ISetCustomTooltipColor, IConnectInterfaces, IUsesMenuOrigins
	{
		static Color s_FrameOpaqueColor;
		static Color s_SemiTransparentFrameColor;
		static Vector3 s_ActivePosition;

		public static Vector3 activePosition
		{
			private get { return s_ActivePosition; }
			set
			{
				s_ActivePosition = value;
			}
		}

		const string k_MaterialColorProperty = "_Color";
		const string k_MaterialAlphaProperty = "_Alpha";
		const string k_SelectionToolTipText = "Selection Tool (cannot be closed)";

		public Type toolType
		{
			get
			{
				return m_ToolType;
			}
			set
			{
				m_GradientButton.gameObject.SetActive(true);

				m_ToolType = value;
				if (m_ToolType != null)
				{
					if (isSelectionTool)
					{
						activeButtonCount = 1;
						tooltipText = k_SelectionToolTipText;
						gradientPair = UnityBrandColorScheme.sessionGradient; // Select tool uses session gradientPair
					}
					else
					{
						tooltipText = toolType.Name;

						// Tools other than select fetch a random gradientPair; also used by the device when highlighted
						gradientPair = UnityBrandColorScheme.GetRandomGradient();
					}

					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					activeTool = true;
					m_GradientButton.visible = true;
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
				m_Order = value; // Position of this button in relation to other pinned tool buttons
				m_InactivePosition = s_ActivePosition * ++value; // Additional offset for the button when it is visible and inactive
				activeTool = activeTool;
				const float kSmoothingMax = 50f;
				const int kSmoothingIncreaseFactor = 10;
				//var smoothingFactor = Mathf.Clamp(kSmoothingMax- m_Order * kSmoothingIncreaseFactor, 0f, kSmoothingMax);
				//m_SmoothMotion.SetPositionSmoothing(smoothingFactor);
				//m_SmoothMotion.SetRotationSmoothing(smoothingFactor);
				//this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition());
				m_LeftPinnedToolActionButton.visible = false;
				m_RightPinnedToolActionButton.visible = false;

				// We move in counter-clockwise direction
				// Account for the input & position phase offset, based on the number of actions, rotating the menu content to be bottom-centered
				const float kMaxPinnedToolButtonCount = 16; // TODO: add max count support in selectTool/setupPinnedToolButtonsForDevice
				const float kRotationSpacing = 360f / kMaxPinnedToolButtonCount; // dividend should be the count of pinned tool buttons showing at this time
				var phaseOffset = kRotationSpacing * 0.5f - (activeButtonCount * 0.5f) * kRotationSpacing;
				var newTargetRotation = Quaternion.AngleAxis(phaseOffset + kRotationSpacing * m_Order, Vector3.down);
				this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition(newTargetRotation));
				//transform.localRotation = newLocalRotation;
			}
		}

		/// <summary>
		/// GradientPair should be set with new random gradientPair each time a new Tool is associated with this Button
		/// This gradientPair is also used to highlight the input device when appropriate
		/// </summary>
		public GradientPair gradientPair
		{
			get { return m_GradientPair; }
			private set
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
			set
			{
				m_previewToolType = value;

				if (m_previewToolType != null) // Show the highlight if the preview type is valid; hide otherwise
				{
					// Show the grayscale highlight when previewing a tool on this button
					m_GradientButton.highlightGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
					m_GradientButton.SetContent(GetTypeAbbreviation(m_previewToolType));
					tooltipText = "Assign " + m_previewToolType.Name;
					customToolTipHighlightColor = UnityBrandColorScheme.grayscaleSessionGradient;
					this.ShowTooltip(this);
				}
				else
				{
					activeTool = activeTool;
					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					customToolTipHighlightColor = gradientPair;
					this.HideTooltip(this);
					tooltipText = isSelectionTool ? tooltipText = k_SelectionToolTipText : toolType.Name;
				}

				m_GradientButton.highlighted = m_previewToolType != null;
			}
		}

		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				if (m_AlternateMenuOrigin == value)
					return;

				m_AlternateMenuOrigin = value;
				transform.SetParent(m_AlternateMenuOrigin);
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
			}
		}

		[SerializeField]
		GradientButton m_GradientButton;

		[SerializeField]
		SmoothMotion m_SmoothMotion;

		[SerializeField]
		PinnedToolActionButton m_LeftPinnedToolActionButton;

		[SerializeField]
		PinnedToolActionButton m_RightPinnedToolActionButton;

		[SerializeField]
		Transform m_ContentContainer;

		[SerializeField]
		Collider m_RootCollider;

		[SerializeField]
		MeshRenderer m_FrameRenderer;

		[SerializeField]
		MeshRenderer m_InsetMeshRenderer;

		[SerializeField]
		Transform m_TooltipTarget;

		[SerializeField]
		Transform m_TooltipSource;

		string m_TooltipText;
		Coroutine m_PositionCoroutine;
		Coroutine m_VisibilityCoroutine;
		Coroutine m_HighlightCoroutine;
		Vector3 m_InactivePosition; // Inactive button offset from the main menu activator
		Transform m_AlternateMenuOrigin;
		Type m_previewToolType;
		GradientPair m_GradientPair;
		int m_Order;
		Type m_ToolType;
		Material m_FrameMaterial;
		bool m_Highlighted;
		Material m_InsetMaterial;

		public string tooltipText { get { return tooltip != null ? tooltip.tooltipText : m_TooltipText; } set { m_TooltipText = value; } }
		public Transform tooltipTarget { get { return m_TooltipTarget; } }
		public Transform tooltipSource { get { return m_TooltipSource; } }
		public TextAlignment tooltipAlignment { get; private set; }
		public Transform rayOrigin { get; set; }
		public Node node { get; set; }
		public ITooltip tooltip { private get; set; } // Overrides text
		public Action<ITooltip> showTooltip { private get; set; }
		public Action<ITooltip> hideTooltip { private get; set; }
		public GradientPair customToolTipHighlightColor { get; set; }
		public bool isSelectionTool { get { return m_ToolType != null && m_ToolType == typeof(Tools.SelectionTool); } }
		public Action<Transform, PinnedToolButton> DeletePinnedToolButton { get; set; }
		public int activeButtonCount { get; set; }
		public Transform menuOrigin { get; set; }
		public Action<Transform, bool> highlightPinnedToolButtons { get; set; }

		private bool activeTool
		{
			get { return m_Order == 0; }
			set
			{
				m_GradientButton.normalGradientPair = value ? gradientPair : UnityBrandColorScheme.grayscaleSessionGradient;
				m_GradientButton.highlightGradientPair = value ? UnityBrandColorScheme.grayscaleSessionGradient : gradientPair;
				m_GradientButton.invertHighlightScale = value;
				m_GradientButton.highlighted = true;
				m_GradientButton.highlighted = false;
			}
		}

		public bool highlighted
		{
			set
			{
				//if (m_Highlighted == value || !gameObject.activeSelf)
					//return;

				this.RestartCoroutine(ref m_HighlightCoroutine, AnimateSemiTransparent(!value));
			}

			//get { return m_Highlighted; }
		}

		void Start()
		{
			//m_GradientButton.onClick += SelectTool; // TODO remove after action button refactor

			Debug.LogWarning("Hide (L+R) pinned tool action buttons if button is the main menu button Hide select action button if button is in the first position (next to menu button)");

			transform.parent = alternateMenuOrigin;

			if (m_ToolType == null)
			{
				//transform.localPosition = m_InactivePosition;
				m_GradientButton.gameObject.SetActive(false);
			}
			else
			{
				//transform.localPosition = activePosition;
			}

			var tooltipSourcePosition = new Vector3(node == Node.LeftHand ? -0.01267f : 0.01267f, tooltipSource.localPosition.y, 0);
			var tooltipXOffset = node == Node.LeftHand ? -0.05f : 0.05f;
			tooltipSource.localPosition = tooltipSourcePosition;
			tooltipAlignment = node == Node.LeftHand ? TextAlignment.Right : TextAlignment.Left;
			m_TooltipTarget.localPosition = new Vector3(tooltipXOffset, tooltipSourcePosition.y, tooltipSourcePosition.z);
			this.ConnectInterfaces(m_SmoothMotion);

			m_FrameMaterial = MaterialUtils.GetMaterialClone(m_FrameRenderer);
			var frameMaterialColor = m_FrameMaterial.color;
			s_FrameOpaqueColor = new Color(frameMaterialColor.r, frameMaterialColor.g, frameMaterialColor.b, 1f);
			s_SemiTransparentFrameColor = new Color(s_FrameOpaqueColor.r, s_FrameOpaqueColor.g, s_FrameOpaqueColor.b, 0.5f);
			m_FrameMaterial.SetColor(k_MaterialColorProperty, s_SemiTransparentFrameColor);

			m_InsetMaterial = MaterialUtils.GetMaterialClone(m_InsetMeshRenderer);
			//m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, 0f);

			m_GradientButton.hoverEnter += BackgroundHoverEnter; // Display the foreground button actions
			m_GradientButton.hoverExit += ActionButtonHoverExit;

			m_LeftPinnedToolActionButton.clicked = ActionButtonClicked;
			m_LeftPinnedToolActionButton.hoverEnter = HoverButton;
			m_LeftPinnedToolActionButton.hoverExit = ActionButtonHoverExit;
			m_RightPinnedToolActionButton.clicked = ActionButtonClicked;
			m_RightPinnedToolActionButton.hoverEnter = HoverButton;
			m_RightPinnedToolActionButton.hoverExit = ActionButtonHoverExit;

			// Assign the select action button to the side closest to the opposite hand, that allows the arrow to also point in the direction the
			var leftHand = node == Node.LeftHand;
			m_RightPinnedToolActionButton.buttonType = leftHand ? PinnedToolActionButton.ButtonType.SelectTool : PinnedToolActionButton.ButtonType.Close;
			m_LeftPinnedToolActionButton.buttonType = leftHand ? PinnedToolActionButton.ButtonType.Close : PinnedToolActionButton.ButtonType.SelectTool;

			m_RightPinnedToolActionButton.rotateIcon = leftHand ? false : true;
			m_LeftPinnedToolActionButton.rotateIcon = leftHand ? false : true;

			m_LeftPinnedToolActionButton.visible = false;
			m_RightPinnedToolActionButton.visible = false;

			m_LeftPinnedToolActionButton.mainButtonCollider = m_RootCollider;
			m_RightPinnedToolActionButton.mainButtonCollider = m_RootCollider;

			//m_ButtonCollider.enabled = true;
			//m_GradientButton.click += OnClick;
			//m_GradientButton.gameObject.SetActive(false);
		}

		void SelectTool()
		{
			this.SelectTool(rayOrigin, m_ToolType); // SelectTool will set button order to 0
			activeTool = activeTool;
			//SetButtonGradients(this.SelectTool(rayOrigin, m_ToolType));
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

		IEnumerator AnimatePosition(Quaternion targetRotation)
		{
			var duration = 0f;
			//var currentPosition = transform.localPosition;
			//var targetPosition = activeTool ? activePosition : m_InactivePosition;
			var currentRotation = transform.localRotation;
			while (duration < 1)
			{
				duration += Time.unscaledDeltaTime * 6;
				var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), 3);
				transform.localRotation = Quaternion.Lerp(currentRotation, targetRotation, durationShaped);
				CorrectIconRotation();
				//transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, durationShaped);
				yield return null;
			}

			//transform.localPosition = targetPosition;
			transform.localRotation = targetRotation;
			CorrectIconRotation();
			m_PositionCoroutine = null;
		}

		void BackgroundHoverEnter ()
		{
			//if (!m_LeftPinnedToolActionButton.highlighted && !m_RightPinnedToolActionButton.highlighted)
			//{
				Debug.LogError("<color=green>Background button was hovered, now triggereing the foreground action button visuals</color>");
				//m_RootCollider.enabled = false;
				m_GradientButton.highlighted = true;
				//m_GradientButton.visible = false;

				//Debug.LogWarning("Handle for disabled buttons not being shown, ie the promotote(green) button on the first/selected tool");

			HoverButton();
			//m_ButtonCollider.enabled = false;
			//}
		}

		void HoverButton()
		{
			if (isSelectionTool)
			{
				if (activeTool)
				{
					m_RightPinnedToolActionButton.visible = false;
					m_LeftPinnedToolActionButton.visible = false;
					m_RootCollider.enabled = true;
				}
				else
				{
					m_RightPinnedToolActionButton.visible = m_RightPinnedToolActionButton.buttonType == PinnedToolActionButton.ButtonType.SelectTool ? true : false;
					m_LeftPinnedToolActionButton.visible = m_LeftPinnedToolActionButton.buttonType == PinnedToolActionButton.ButtonType.SelectTool ? true : false;
				}
			} else
			{
				// Hide the select action button if this tool button is already the selected tool, else show the close button
				m_RightPinnedToolActionButton.visible = m_RightPinnedToolActionButton.buttonType == PinnedToolActionButton.ButtonType.SelectTool ? !activeTool : true;
				m_LeftPinnedToolActionButton.visible = m_LeftPinnedToolActionButton.buttonType == PinnedToolActionButton.ButtonType.SelectTool ? !activeTool : true;
			}

			highlightPinnedToolButtons(rayOrigin, true);
		}

		void ActionButtonClicked(PinnedToolActionButton button)
		{
			Debug.LogError("Action Button clicked!");
			if (button.buttonType == PinnedToolActionButton.ButtonType.SelectTool)
			{
				m_LeftPinnedToolActionButton.highlighted = false;
				m_RightPinnedToolActionButton.highlighted = false;
				ActionButtonHoverExit();
				SelectTool();
			}
			else
			{
				if (!isSelectionTool)
					this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateClose());
				else
					Debug.LogError("<color=red>CANNOT DELETE THE SELECT TOOL!!!!!</color>");
			}
		}

		void ActionButtonHoverExit()
		{
			Debug.LogWarning("<color=orange>ActionButtonHoverExit : </color>" + name + " : " + toolType);
			// in this case display the hover state for the gradient button, then enable visibility for each of the action buttons

			// Hide both action buttons if the user is no longer hovering over the button
			if (!m_LeftPinnedToolActionButton.highlighted && !m_RightPinnedToolActionButton.highlighted)
			{
				Debug.LogWarning("<color=green>!!!</color>");
				//m_ButtonCollider.enabled = true;
				m_LeftPinnedToolActionButton.visible = false;
				m_RightPinnedToolActionButton.visible = false;
				//m_GradientButton.visible = true;
				m_GradientButton.highlighted = false;
				highlightPinnedToolButtons(rayOrigin, false);
			}

			m_GradientButton.UpdateMaterialColors();
		}

		void CloseButton()
		{
			// TODO add full close functionality
			gameObject.SetActive(false);

			// perform a graceful hiding of visuals, then destroy this button gameobject
		}

		IEnumerator AnimateClose()
		{
			this.HideTooltip(this);
			m_RootCollider.enabled = false;
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
			DeletePinnedToolButton(rayOrigin, this);
			ObjectUtils.Destroy(gameObject, 0.1f);
		}

		public void CorrectIconRotation()
		{
			const float kIconLookForwardOffset = 0.5f;
			var iconLookDirection = m_ContentContainer.transform.position + transform.parent.forward * kIconLookForwardOffset; // set a position offset above the icon, regardless of the icon's rotation
			m_ContentContainer.LookAt(iconLookDirection);
			m_ContentContainer.localEulerAngles = new Vector3(0f, 0f, m_ContentContainer.localEulerAngles.z);
			var angle = m_ContentContainer.localEulerAngles.z;
			m_TooltipTarget.localEulerAngles = new Vector3(90f, 0f, angle);

			var yaw = transform.localRotation.eulerAngles.y;
			tooltipAlignment = yaw > 90 && yaw <= 270 ? TextAlignment.Right : TextAlignment.Left;
		}

		IEnumerator AnimateSemiTransparent(bool makeSemiTransparent)
		{
			Debug.LogWarning("<color=blue>AnimateSemiTransparent : </color>" + makeSemiTransparent);
			const float kFasterMotionMultiplier = 2f;
			var transitionAmount = Time.unscaledDeltaTime;
			var positionWait = (order + 1) * 0.25f; // pad the order index for a faster start to the transition
			//var semiTransparentTargetScale = new Vector3(0.9f, 0.15f, 0.9f);
			var currentFrameColor = m_FrameMaterial.color;
			var transparentFrameColor = new Color (s_FrameOpaqueColor.r, s_FrameOpaqueColor.g, s_FrameOpaqueColor.b, 0f);
			var targetFrameColor = makeSemiTransparent ? s_SemiTransparentFrameColor : s_FrameOpaqueColor;
			var currentInsetAlpha = m_InsetMaterial.GetFloat(k_MaterialAlphaProperty);
			var targetInsetAlpha = makeSemiTransparent ? 0.25f : 1f;
			//var currentIconColor = m_IconMaterial.GetColor(k_MaterialColorProperty);
			//var targetIconColor = makeSemiTransparent ? s_SemiTransparentFrameColor : Color.white;
			//var currentInsetScale = m_MenuInset.localScale;
			//var targetInsetScale = makeSemiTransparent ? m_HighlightedInsetLocalScale * 4 : m_VisibleInsetLocalScale;
			//var currentIconScale = m_IconContainer.localScale;
			//var semiTransparentTargetIconScale = Vector3.one * 1.5f;
			//var targetIconScale = makeSemiTransparent ? semiTransparentTargetIconScale : Vector3.one;
			while (transitionAmount < 1)
			{
				m_FrameMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentFrameColor, transparentFrameColor, transitionAmount));
				//m_MenuInset.localScale = Vector3.Lerp(currentInsetScale, targetInsetScale, transitionAmount * 2f);
				//m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, Mathf.Lerp(currentInsetAlpha, targetInsetAlpha, transitionAmount));
				//m_IconMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentIconColor, targetIconColor, transitionAmount));
				//var shapedTransitionAmount = Mathf.Pow(transitionAmount, makeSemiTransparent ? 2 : 1) * kFasterMotionMultiplier;
				//m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconScale, shapedTransitionAmount);
				transitionAmount += Time.unscaledDeltaTime * 4f;
				CorrectIconRotation();
				yield return null;
			}

			m_FrameMaterial.SetColor(k_MaterialColorProperty, targetFrameColor);
			//m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, targetInsetAlpha);
			//m_IconMaterial.SetColor(k_MaterialColorProperty, targetIconColor);
			//m_MenuInset.localScale = targetInsetScale;
			//m_IconContainer.localScale = targetIconScale;
		}
	}
}
#endif
