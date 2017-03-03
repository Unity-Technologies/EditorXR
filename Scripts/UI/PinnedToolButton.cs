#if UNITY_EDITOR
using System;
using System.Text;
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

				m_GradientButton.gameObject.SetActive(true);

				m_ToolType = value;
				if (m_ToolType != null)
				{
					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					SetButtonGradients(true);
					m_GradientButton.visible = true;
				}
			}
		}
		Type m_ToolType;

		[SerializeField]
		GradientButton m_GradientButton;

		public Transform rayOrigin { get; set; }
		public Func<Transform, Type, bool> selectTool { private get; set; }

		void Start()
		{
			m_GradientButton.onClick += OnClick;
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
				m_GradientButton.normalGradientPair = UnityBrandColorScheme.sessionGradient;
				m_GradientButton.highlightGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
			}
			else
			{
				m_GradientButton.normalGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
				m_GradientButton.highlightGradientPair = UnityBrandColorScheme.sessionGradient;
			}
		}
	}
}
#endif
