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

	protected UnityEvent m_OnCloseKeyboard = new UnityEvent();

	[SerializeField]
	private Transform m_KeyboardAnchorTransform;

	[SerializeField]
	protected Text m_TextComponent;

	[SerializeField]
	private int m_CharacterLimit = 10;

	public delegate char OnValidateInput(string text, int charIndex, char addedChar);
	[SerializeField]
	private OnValidateInput m_OnValidateInput;
	public OnValidateInput onValidateInput { get { return m_OnValidateInput; } set { m_OnValidateInput = value; } }

	[SerializeField]
	private InputField.SubmitEvent m_OnEndEdit = new InputField.SubmitEvent();

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
	[HideInInspector]
	[SerializeField] // Serialized so that this remains set after cloning
	protected string m_Text = string.Empty;

	protected override void OnEnable()
	{
		base.OnEnable();

		if (m_Text == null)
			m_Text = string.Empty;

		if (m_TextComponent != null)
		{
			UpdateLabel();
		}
	}

	public void ForceUpdateLabel()
	{
		UpdateLabel();
	}

	public virtual void ClearLabel()
	{
		
	}
	public void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			if (m_Open)
				Close();
			else
				Open();
		}
	}

	public void OnSubmit(BaseEventData eventData)
	{
		//		throw new System.NotImplementedException();
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

	}

	private void Open()
	{
		if (m_Open) return;
		m_Open = true;

		m_Keyboard = spawnKeyboard();
		// Instantiate keyboard here
		if (m_Keyboard != null)
		{
			m_Keyboard.gameObject.SetActive(true);
			m_Keyboard.transform.SetParent(transform, true);
			m_Keyboard.transform.position = m_KeyboardAnchorTransform.position;
			m_Keyboard.transform.rotation = m_KeyboardAnchorTransform.rotation;

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
			case (int)KeyboardButton.SpecialKeyType.None:
				return;
			case (int)KeyboardButton.SpecialKeyType.Backspace:
				Backspace();
				return;
			case (int)KeyboardButton.SpecialKeyType.Tab:
				return;
			case (int)KeyboardButton.SpecialKeyType.CarriageReturn:
				Return();
				return;
			case (int)KeyboardButton.SpecialKeyType.Space:
				Space();
				return;
		}

		if (IsValid(keyCode))
			Append(keyCode);
	}

	protected virtual bool IsValid(char c)
	{
		return m_TextComponent.font.HasCharacter(c);
	}

	protected abstract void Append(char c);
	protected abstract void Backspace();
	protected abstract void Return();
	protected abstract void Space();
}
