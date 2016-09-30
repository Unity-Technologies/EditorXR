using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public abstract class RayInputField : Selectable, ISubmitHandler, IPointerClickHandler
{
	public SelectionFlags selectionFlags
	{
		get { return m_SelectionFlags; }
		set { m_SelectionFlags = value; }
	}
	[SerializeField]
	[FlagsProperty]
	protected SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

	public Func<KeyboardUI> spawnKeyboard;
	protected KeyboardUI m_Keyboard;

	[Serializable]
	public class OnChangeEvent : UnityEvent<string> { }
	public OnChangeEvent onValueChanged { get { return m_OnValueChanged; } }
	[SerializeField]
	private OnChangeEvent m_OnValueChanged = new OnChangeEvent();

	[SerializeField]
	protected Text m_TextComponent;

	[SerializeField]
	private int m_CharacterLimit = 10;

	private bool m_Open;

	public string text
	{
		get
		{
			return m_Text;
		}
		set
		{
			if (m_Text == value)
				return;
			if (value == null)
				value = "";

			m_Text = m_CharacterLimit > 0 && value.Length > m_CharacterLimit ? value.Substring(0, m_CharacterLimit) : value;
		}
	}
	protected string m_Text = string.Empty;

	protected override void OnEnable()
	{
		base.OnEnable();

		if (m_Text == null)
			m_Text = string.Empty;

		if (m_TextComponent != null)
			UpdateLabel();
	}

	/// <summary>
	/// Update the label with the current text
	/// </summary>
	public void ForceUpdateLabel()
	{
		UpdateLabel();
	}

	/// <summary>
	/// Clear all text from the field
	/// </summary>
	public virtual void ClearLabel()
	{
		Clear();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			if (m_Open)
				Close();
			else
			{
				if (rayEventData != null)
					Open(rayEventData.rayOrigin.position);
			}
		}
	}

	public void OnSubmit(BaseEventData eventData)
	{
		//
	}

	public override void OnSelect(BaseEventData eventData)
	{
		//
	}

	protected void SendOnValueChangedAndUpdateLabel()
	{
		SendOnValueChanged();
		UpdateLabel();
	}

	protected void SendOnValueChanged()
	{
		if (onValueChanged != null)
			onValueChanged.Invoke(text);
	}

	protected void UpdateLabel()
	{
		if (m_TextComponent != null && m_TextComponent.font != null)
			m_TextComponent.text = m_Text;
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		//
	}

	protected virtual void Open(Vector3 position)
	{
		if (m_Open) return;
		m_Open = true;

		m_Keyboard = spawnKeyboard();
		if (m_Keyboard != null)
		{
			m_Keyboard.gameObject.SetActive(true);
			m_Keyboard.transform.position = position + Vector3.up * 0.01f;
			m_Keyboard.transform.rotation = transform.rotation;
			m_Keyboard.Setup(OnKeyPress);
		}
	}

	protected virtual void Close()
	{
		m_Open = false;

		if (m_Keyboard == null) return;

		m_Keyboard.gameObject.SetActive(false);
		m_Keyboard = null;
	}

	protected void OnKeyPress(char keyCode)
	{
		switch ((int)keyCode)
		{
			case (int)KeyCode.None:
				return;
			case (int)KeyCode.Backspace:
				Backspace();
				return;
			case (int)KeyCode.Tab:
				Tab();
				return;
			case (int)KeyCode.Clear:
				Clear();
				return;
			case '\n':
			case (int)KeyCode.Return:
				Return();
				return;
			case (int)KeyCode.Escape:
				Escape();
				return;
			case (int)KeyCode.Space:
				Space();
				return;
			case (int)KeyCode.LeftShift:
			case (int)KeyCode.RightShift:
				Shift();
				return;
			case (int)KeyCode.CapsLock:
				CapsLock();
				return;
		}

		if (IsValid(keyCode))
			Append(keyCode);
	}

	protected virtual bool IsValid(char c)
	{
		return m_TextComponent.font.HasCharacter(c);
	}

	protected virtual void Escape()
	{
		Close();
	}

	protected virtual void Clear()
	{
		m_Text = "";
		SendOnValueChangedAndUpdateLabel();
	}

	protected abstract void Append(char c);
	protected abstract void Backspace();
	protected abstract void Tab();
	protected abstract void Return();
	protected abstract void Space();
	protected abstract void Shift();
	protected abstract void CapsLock();
}
