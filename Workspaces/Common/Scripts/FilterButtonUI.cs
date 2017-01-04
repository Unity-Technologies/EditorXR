using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.Experimental.EditorVR.UI.Button;

public class FilterButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	private const float kHoverAlpha = 1;
	private const float kNormalAlpha = 0.95f;

	public Button button { get { return m_Button; } }
	[SerializeField]
	private Button m_Button;

	[SerializeField]
	private Image m_EyePanel;

	[SerializeField]
	private Image m_Eye;

	[SerializeField]
	private Image m_TextPanel;

	public Text text { get { return m_Text; } }
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
		c.a = kHoverAlpha;
		m_EyePanel.color = c;
		m_TextPanel.color = c;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Color c = m_EyePanel.color;
		c.a = kNormalAlpha;
		m_EyePanel.color = c;
		m_TextPanel.color = c;
	}
}