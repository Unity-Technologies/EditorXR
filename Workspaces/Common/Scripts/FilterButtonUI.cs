#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEditor.Experimental.EditorVR.UI.Button;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class FilterButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private const float k_HoverAlpha = 1;
		private const float k_NormalAlpha = 0.95f;

		public Button button
		{
			get { return m_Button; }
		}

		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private Image m_EyePanel;

		[SerializeField]
		private Image m_Eye;

		[SerializeField]
		private Image m_TextPanel;

		public Text text
		{
			get { return m_Text; }
		}

		[SerializeField]
		private Text m_Text;

		public Color color
		{
			set
			{
				m_Eye.color = value;
				m_Text.color = value;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			Color c = m_EyePanel.color;
			c.a = k_HoverAlpha;
			m_EyePanel.color = c;
			m_TextPanel.color = c;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Color c = m_EyePanel.color;
			c.a = k_NormalAlpha;
			m_EyePanel.color = c;
			m_TextPanel.color = c;
		}
	}
}
#endif
