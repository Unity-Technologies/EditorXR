using UnityEngine;

public class StandardInputField : RayInputField
{
	public enum LineType
	{
		SingleLine,
		MultiLine,
	}

	[SerializeField]
	private LineType m_LineType = LineType.SingleLine;

	private bool m_CapsLock;
	private bool m_Shift;

	protected override void Append(char c)
	{
		var len = m_Text.Length;

		if (m_CapsLock && !m_Shift || !m_CapsLock && m_Shift)
			c = char.ToUpper(c);
		else if (m_CapsLock && m_Shift || !m_CapsLock && !m_Shift)
			c = char.ToLower(c);
		// Deactivate shift after pressing a key
		if (m_Shift) Shift();

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

	protected override void Tab()
	{
		if (m_LineType == LineType.SingleLine) return;

		const char kTab = '\t';
		text += kTab;

		SendOnValueChangedAndUpdateLabel();
	}

	protected override void Return()
	{
		if (m_LineType == LineType.SingleLine) return;

		const char kNewline = '\n';
		const string kLineBreak = "<br>";
		text += kNewline;
		text = text.Replace(kLineBreak, kNewline.ToString());

		SendOnValueChangedAndUpdateLabel();
	}

	protected override void Space()
	{
		var len = m_Text.Length;

		const string kWhiteSpace = " ";
		text += kWhiteSpace;

		if (len != m_Text.Length)
			SendOnValueChangedAndUpdateLabel();
	}

	protected override void Shift()
	{
		m_Shift = !m_Shift;

		UpdateKeyText();
	}

	protected override void CapsLock()
	{
		m_CapsLock = !m_CapsLock;

		UpdateKeyText();
	}

	private void UpdateKeyText()
	{
		if (m_CapsLock && !m_Shift || !m_CapsLock && m_Shift)
			m_Keyboard.ActivateShiftModeOnKeys();
		else if (m_CapsLock && m_Shift || !m_CapsLock && !m_Shift)
			m_Keyboard.DeactivateShiftModeOnKeys();
	}
}
