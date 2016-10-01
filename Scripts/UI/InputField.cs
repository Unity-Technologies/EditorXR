using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.UI
{
	public abstract class InputField : Selectable, IPointerClickHandler
	{
		private static readonly Vector3 kKeyboardPositionOffset = new Vector3(0.10f, 0.01f, 0.05f);
		private static readonly Quaternion kKeyboardRotationOffset = Quaternion.AngleAxis(30, Vector3.up);

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
		[HideInInspector]
		[SerializeField] // Serialized so that this remains set after cloning
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
				if (rayEventData != null)
					ToggleKeyboard(rayEventData.rayOrigin.position);
				else if (m_Open)
					Close();
			}
		}

		public void ToggleKeyboard(Vector3 position)
		{
			if (m_Open)
				Close();
			else
				Open(position);
		}

		public override void OnSelect(BaseEventData eventData)
		{
			// Don't do base functionality
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

		protected virtual void Open(Vector3 position)
		{
			if (m_Open) return;
			m_Open = true;

			m_Keyboard = spawnKeyboard();
			if (m_Keyboard != null)
			{
				m_Keyboard.gameObject.SetActive(true);
				m_Keyboard.transform.position = position + kKeyboardPositionOffset;
				m_Keyboard.transform.rotation = kKeyboardRotationOffset;
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
			const KeyCode kNewline = (KeyCode)'\n';
			switch ((KeyCode)keyCode)
			{
				case KeyCode.None:
					return;
				case KeyCode.Backspace:
					Backspace();
					return;
				case KeyCode.Tab:
					Tab();
					return;
				case KeyCode.Clear:
					Clear();
					return;
				case kNewline:
				case KeyCode.Return:
					Return();
					return;
				case KeyCode.Escape:
					Escape();
					return;
				case KeyCode.Space:
					Space();
					return;
				case KeyCode.LeftShift:
				case KeyCode.RightShift:
					Shift();
					return;
				case KeyCode.CapsLock:
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

}