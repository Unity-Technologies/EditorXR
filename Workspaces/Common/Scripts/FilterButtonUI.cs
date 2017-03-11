#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEditor.Experimental.EditorVR.UI.Button;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class FilterButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		const float k_HoverAlpha = 1;
		const float k_NormalAlpha = 0.95f;

		public Button button
		{
			get { return m_Button; }
		}

		[SerializeField]
		Button m_Button;

		[SerializeField]
		Image m_EyePanel;

		[SerializeField]
		Image m_Eye;

		[SerializeField]
		Image m_TextPanel;

		public Text text
		{
			get { return m_Text; }
		}

		[SerializeField]
		Text m_Text;

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
			var c = m_EyePanel.color;
			c.a = k_HoverAlpha;
			m_EyePanel.color = c;
			m_TextPanel.color = c;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			var c = m_EyePanel.color;
			c.a = k_NormalAlpha;
			m_EyePanel.color = c;
			m_TextPanel.color = c;
		}
	}
}
#endif
