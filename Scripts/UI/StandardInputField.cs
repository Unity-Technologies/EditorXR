using UnityEngine;
using UnityEngine.UI;

public class StandardInputField : RayInputField
{
	public enum LineType
	{
		SingleLine,
		MultiLineSubmit,
		MultiLineNewline
	}

	[SerializeField]
	private LineType m_LineType = LineType.SingleLine;

	[SerializeField]
	protected Graphic m_Placeholder;

	private bool m_Shift;
	private bool m_CapsLock;

	protected override void Append(char c)
	{
		var len = m_Text.Length;

		if (m_LineType == LineType.SingleLine && (c == '\n' || c == '\t')) return;

		text += c;

		if (len != m_Text.Length)
			SendOnValueChangedAndUpdateLabel();
	}

	protected override void Backspace()
	{
		if (m_Text.Length == 0) return;

		m_Text = m_Text.Remove(m_Text.Length - 1);

		SendOnValueChangedAndUpdateLabel();
	}

	protected override void Return()
	{
//		if (c == '\r' || (int)c == 3)
//			c = '\n';
		//TODO multiline
		SendOnValueChangedAndUpdateLabel();
	}

	protected override void Space()
	{
		var len = m_Text.Length;

		text += " ";

		if (len != m_Text.Length)
			SendOnValueChangedAndUpdateLabel();
	}

//	private void Shift()
//	{
//		foreach (var button in m_Keyboard.buttons)
//		{
//			
//		}
//	}
}
