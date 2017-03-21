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
	public sealed class PinnedToolButton : MonoBehaviour, ISelectTool, ITooltip, ITooltipPlacement, ISetTooltipVisibility, ISetCustomTooltipColor
	{
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

				Debug.LogError("PinnedToolButton setting TYPE : <color=green>" + value.ToString() + "</color>");
				m_GradientButton.gameObject.SetActive(true);

				m_ToolType = value;
				if (m_ToolType != null)
				{
					if (isSelectTool)
					{
						tooltipText = "Selection Tool (cannot be closed)";
						gradientPair = UnityBrandColorScheme.sessionGradient; // Select tool uses session gradientPair
						activeTool = true;
					}
					else
					{
						tooltipText = toolType.Name;

						// Tools other than select fetch a random gradientPair; also used by the device when highlighted
						gradientPair = UnityBrandColorScheme.GetRandomGradient();
					}

					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					SetButtonGradients(true);
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

		public bool activeTool // use this externally to make visible & move a button to the active/inactive position
		{
			get { return m_ActiveTool; }
			set
			{
				//if (value == m_ActiveTool)
					//return;

				m_ActiveTool = value;

				if (m_ToolType != null)
					Debug.LogError(m_ToolType.ToString() + " : <color=purple>PinnedToolButton ACTIVE : </color>" + value);

				if (m_ActiveTool)
				{
					if (m_ToolType == null)
					{
						//this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());
						gameObject.SetActive(m_ActiveTool);
					}

					// Perform re-position coroutine here
					this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition());
					transform.localPosition = activePosition;
				}
				else
				{
					this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition());
					//gameObject.SetActive(false);
					transform.localPosition = m_InactivePosition;
				}

				SetButtonGradients(m_ActiveTool);
			}
		}
		bool m_ActiveTool;

		public Vector3 activePosition
		{
			private get { return m_ActivePosition; }
			set
			{
				m_ActivePosition = value;
				m_InactivePosition = value * 2f; // additional offset for the button when it is visible and inactive
			}
		}
		Vector3 m_ActivePosition;

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
					SetButtonGradients(activeTool);
					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					customToolTipHighlightColor = gradientPair;
					hideTooltip(this);
					tooltipText = isSelectTool ? tooltipText = "Selection Tool (cannot be closed)" : toolType.Name;
				}

				m_GradientButton.highlighted = m_previewToolType != null;
			}
		}
		Type m_previewToolType;

		public string tooltipText { get { return tooltip != null ? tooltip.tooltipText : m_TooltipText; } set { m_TooltipText = value; } }
		string m_TooltipText;

		[SerializeField]
		GradientButton m_GradientButton;

		public Transform tooltipTarget { get { return m_TooltipTarget; } }
		[SerializeField]
		Transform m_TooltipTarget;

		public Transform tooltipSource { get { return m_TooltipSource; } }
		[SerializeField]
		Transform m_TooltipSource;

		public TextAlignment tooltipAlignment { get; private set; }
		public Transform rayOrigin { get; set; }
		public Func<Transform, Type, bool> selectTool { private get; set; }
		public Node node { get; set; }
		public ITooltip tooltip { private get; set; } // Overrides text
		public Action<ITooltip> showTooltip { private get; set; }
		public Action<ITooltip> hideTooltip { private get; set; }
		public GradientPair customToolTipHighlightColor { get; set; }

		bool isSelectTool
		{
			get { return m_ToolType != null && m_ToolType == typeof(Tools.SelectionTool); }
		}

		Vector3 m_InactivePosition; // Inactive button offset from the main menu activator
		Coroutine m_PositionCoroutine;

		void Start()
		{
			m_GradientButton.onClick += OnClick;

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
		}

		void OnClick()
		{
			selectTool(rayOrigin, m_ToolType);
			SetButtonGradients(activeTool);
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

		void SetButtonGradients(bool active)
		{
			if (active)
			{
				m_GradientButton.normalGradientPair = gradientPair;
				m_GradientButton.highlightGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
				m_GradientButton.highlighted = true;
				m_GradientButton.highlighted = false;
			}
			else
			{
				m_GradientButton.normalGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
				m_GradientButton.highlightGradientPair = gradientPair;
				m_GradientButton.highlighted = true;
				m_GradientButton.highlighted = false;
			}
		}

		IEnumerator AnimatePosition()
		{
			var duration = 0f;
			var currentPosition = transform.localPosition;
			var targetPosition = m_ActiveTool ? activePosition : m_InactivePosition;
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
	}
}
#endif
