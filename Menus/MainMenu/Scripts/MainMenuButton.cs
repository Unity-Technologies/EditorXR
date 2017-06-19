#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuButton : MonoBehaviour, ITooltip
	{
		public Button button { get { return m_Button; } }
		[SerializeField]
		Button m_Button;

		[SerializeField]
		Text m_ButtonDescription;
		[SerializeField]
		Text m_ButtonTitle;

		Color m_OriginalColor;

		public string tooltipText
		{
			get
			{
				return tooltip != null ? tooltip.tooltipText : null;
			}
		}

		public ITooltip tooltip { private get; set; }

		public bool selected
		{
			set
			{
				if (value)
				{
					m_Button.transition = Selectable.Transition.None;
					m_Button.targetGraphic.color = m_Button.colors.highlightedColor;
				}
				else
				{
					m_Button.transition = Selectable.Transition.ColorTint;
					m_Button.targetGraphic.color = m_OriginalColor;
				}
			}
		}

		void Awake()
		{
			m_OriginalColor = m_Button.targetGraphic.color;
		}

		public void SetData(string name, string description)
		{
			m_ButtonTitle.text = name;
			m_ButtonDescription.text = description;
		}
	}
}
#endif
