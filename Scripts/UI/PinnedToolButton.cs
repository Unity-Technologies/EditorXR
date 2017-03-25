#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public sealed class PinnedToolButton : MonoBehaviour, ISelectTool, ITooltip, ITooltipPlacement, ISetTooltipVisibility, ISetCustomTooltipColor, IConnectInterfaces
	{
		public static Vector3 activePosition
		{
			private get { return s_ActivePosition; }
			set
			{
				s_ActivePosition = value;
			}
		}
		static Vector3 s_ActivePosition;

		const string k_SelectionToolTipText = "Selection Tool (cannot be closed)";

		public Type toolType
		{
			get
			{
				return m_ToolType;
			}

			set
			{
				if (m_ToolType == value)
					return;

				m_GradientButton.gameObject.SetActive(true);

				m_ToolType = value;
				if (m_ToolType != null)
				{
					if (isSelectTool)
					{
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
		Type m_ToolType;

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
				var smoothingFactor = Mathf.Clamp(kSmoothingMax- m_Order * kSmoothingIncreaseFactor, 0f, kSmoothingMax);
				m_SmoothMotion.SetPositionSmoothing(smoothingFactor);
				m_SmoothMotion.SetRotationSmoothing(smoothingFactor);
				this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition());
			}
		}
		int m_Order;

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
		GradientPair m_GradientPair;

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
					showTooltip(this);
				}
				else
				{
					activeTool = activeTool;
					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					customToolTipHighlightColor = gradientPair;
					hideTooltip(this);
					tooltipText = isSelectTool ? tooltipText = k_SelectionToolTipText : toolType.Name;
				}

				m_GradientButton.highlighted = m_previewToolType != null;
			}
		}
		Type m_previewToolType;

		[SerializeField]
		GradientButton m_GradientButton;

		[SerializeField]
		SmoothMotion m_SmoothMotion;

		[SerializeField]
		PinnedToolActionButton m_LeftPinnedToolActionButton;

		[SerializeField]
		PinnedToolActionButton m_RightPinnedToolActionButton;

		public Transform tooltipTarget { get { return m_TooltipTarget; } }
		[SerializeField]
		Transform m_TooltipTarget;

		public Transform tooltipSource { get { return m_TooltipSource; } }
		[SerializeField]
		Transform m_TooltipSource;

		public string tooltipText { get { return tooltip != null ? tooltip.tooltipText : m_TooltipText; } set { m_TooltipText = value; } }
		string m_TooltipText;

		public TextAlignment tooltipAlignment { get; private set; }
		public Transform rayOrigin { get; set; }
		public Func<Transform, Type, bool> selectTool { private get; set; }
		public Node node { get; set; }
		public ITooltip tooltip { private get; set; } // Overrides text
		public Action<ITooltip> showTooltip { private get; set; }
		public Action<ITooltip> hideTooltip { private get; set; }
		public GradientPair customToolTipHighlightColor { get; set; }
		public bool isSelectTool { get { return m_ToolType != null && m_ToolType == typeof(Tools.SelectionTool); } }
		public ConnectInterfacesDelegate connectInterfaces { get; set; }

		Coroutine m_PositionCoroutine;
		Vector3 m_InactivePosition; // Inactive button offset from the main menu activator

		bool activeTool
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

		void Start()
		{
			//m_GradientButton.onClick += SelectTool; // TODO remove after action button refactor

			if (m_ToolType == null)
			{
				transform.localPosition = m_InactivePosition;
				m_GradientButton.gameObject.SetActive(false);
			}
			else
			{
				transform.localPosition = activePosition;
			}

			var tooltipSourcePosition = new Vector3(node == Node.LeftHand ? -0.01267f : 0.01267f, tooltipSource.localPosition.y, 0);
			var tooltipXOffset = node == Node.LeftHand ? -0.05f : 0.05f;
			tooltipSource.localPosition = tooltipSourcePosition;
			tooltipAlignment = node == Node.LeftHand ? TextAlignment.Right : TextAlignment.Left;
			m_TooltipTarget.localPosition = new Vector3(tooltipXOffset, tooltipSourcePosition.y, tooltipSourcePosition.z);
			connectInterfaces(m_SmoothMotion);

			m_GradientButton.onHoverEnter += BackgroundHovered; // Display the foreground button actions

			m_LeftPinnedToolActionButton.clicked = ActionButtonClicked;
			m_LeftPinnedToolActionButton.closeButton = CloseButton;
			//m_LeftPinnedToolActionButton.hoverEnter = ActionButtonHoverEnter;
			m_LeftPinnedToolActionButton.hoverExit = ActionButtonHoverExit;
			m_RightPinnedToolActionButton.clicked = ActionButtonClicked;
			m_RightPinnedToolActionButton.closeButton = CloseButton;
			//m_RightPinnedToolActionButton.hoverEnter = ActionButtonHoverEnter;
			m_RightPinnedToolActionButton.hoverExit = ActionButtonHoverExit;

			// Assign the select action button to the side closest to the opposite hand, that allows the arrow to also point in the direction the
			var leftHand = node == Node.LeftHand;
			m_RightPinnedToolActionButton.buttonType = leftHand ? PinnedToolActionButton.ButtonType.SelectTool : PinnedToolActionButton.ButtonType.Close;
			m_LeftPinnedToolActionButton.buttonType = leftHand ? PinnedToolActionButton.ButtonType.Close : PinnedToolActionButton.ButtonType.SelectTool;
		}

		void SelectTool()
		{
			selectTool(rayOrigin, m_ToolType);
			activeTool = activeTool;
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

		IEnumerator AnimatePosition()
		{
			var duration = 0f;
			var currentPosition = transform.localPosition;
			var targetPosition = activeTool ? activePosition : m_InactivePosition;
			while (duration < 1)
			{
				duration += Time.unscaledDeltaTime * 3;
				var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), 4);
				transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, durationShaped);
				yield return null;
			}

			transform.localPosition = targetPosition;
			m_PositionCoroutine = null;
		}

		void BackgroundHovered ()
		{
			Debug.LogError("<color=green>Background button was hovered, now triggereing the foreground action button visuals</color>");
			m_GradientButton.highlighted = true;
			m_GradientButton.visible = false;

			m_LeftPinnedToolActionButton.visible = true;
			m_RightPinnedToolActionButton.visible = true;
		}

		void ActionButtonClicked(PinnedToolActionButton button)
		{
			Debug.LogError("Action Button clicked!");
			if (button.buttonType == PinnedToolActionButton.ButtonType.SelectTool)
				SelectTool();
			else
				button.closeButton();
		}
/*
		void ActionButtonHoverEnter()
		{
			Debug.LogError("<color=green>Action Button hover ENTER event raised!</color>");
			m_LeftPinnedToolActionButton.visible = true;
			m_RightPinnedToolActionButton.visible = true;
		}
*/
		void ActionButtonHoverExit()
		{

			// in this case display the hover state for the gradient button, then enable visibility for each of the action buttons

			// Hide both action buttons if the user is no longer hovering over the button
			if (!m_LeftPinnedToolActionButton.highlighted && !m_RightPinnedToolActionButton.highlighted)
			{
				m_LeftPinnedToolActionButton.visible = false;
				m_RightPinnedToolActionButton.visible = false;
				m_GradientButton.visible = true;
				m_GradientButton.highlighted = false;
			}
		}

		void CloseButton()
		{
			// TODO add full close functionality
			gameObject.SetActive(false);
		}
	}
}
#endif
