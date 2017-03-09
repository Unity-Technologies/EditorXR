#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class PinnedToolButton : MonoBehaviour, ISelectTool
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
					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					SetButtonGradients(true);
					m_GradientButton.visible = true;

					if (isSelectTool)
					{
						m_Tooltip.tooltipText = "Selection TOOL!!!!";
						activeTool = true;
						gradientPair = UnityBrandColorScheme.sessionGradient; // Select tool uses session gradientPair
					}
					else
					{
						m_Tooltip.tooltipText = "NOT SELECTION TOOL!!!";

						// Tools other than select fetch a random gradientPair; also used by the device when highlighted
						gradientPair = UnityBrandColorScheme.GetRandomGradient();
					}
				}
				else
				{
					m_GradientButton.visible = false;
				}
			}
		}
		Type m_ToolType;

		public bool activeTool // use this externally to make visible & move a button to the active/inactive position
		{
			get { return m_ActiveTool; }
			set
			{
				if (value == m_ActiveTool)
					return;

				m_ActiveTool = value;

				if (value)
				{
					if (m_ToolType == null)
					{
						//this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());
						gameObject.SetActive(true);
					}

					// Perform re-position coroutine here
					//this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition(activePosition));
					transform.localPosition = activePosition;
				}
				else
				{
					//this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition(inactivePosition));
					transform.localPosition = inactivePosition;
				}
			}
		}
		bool m_ActiveTool;

		public Vector3 activePosition
		{
			private get { return m_ActivePosition; }
			set
			{
				m_ActivePosition = value;
				inactivePosition = value * 2.25f; // additional offset for the button when it is visible and inactive
			}
		}
		Vector3 m_ActivePosition;

		/// <summary>
		/// gradientPair should be set with new random gradientPair each time a new Tool is associated with this Button
		/// This gradientPair is also used to highlight the input device when appropriate
		/// </summary>
		public GradientPair gradientPair
		{
			get { return _mGradientPair; }
			private set { _mGradientPair = value; }
		}
		GradientPair _mGradientPair;

		[SerializeField]
		GradientButton m_GradientButton;

		[SerializeField]
		Tooltip m_Tooltip;

		public Transform rayOrigin { get; set; }
		public Func<Transform, Type, bool> selectTool { private get; set; }

		bool isSelectTool
		{
			get { return m_ToolType != null && m_ToolType == typeof(Tools.SelectionTool); }
		}

		Vector3 inactivePosition; // Inactive button offset from the main menu activator

		void Start()
		{
			m_GradientButton.onClick += OnClick;

			if (m_ToolType == null)
				m_GradientButton.gameObject.SetActive(false);
		}

		void OnClick()
		{
			SetButtonGradients(selectTool(rayOrigin, m_ToolType));
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
			}
			else
			{
				m_GradientButton.normalGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
				m_GradientButton.highlightGradientPair = gradientPair;
			}
		}
	}
}
#endif
