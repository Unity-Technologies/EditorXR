using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Extensions;

namespace UnityEngine.Experimental.EditorVR.UI
{
	public abstract class InputField : Selectable, ISelectionFlags
	{
		const float kMoveKeyboardTime = 0.2f;
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

		private bool m_KeyboardOpen;

		Coroutine m_MoveKeyboardCoroutine;

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

		protected virtual void UpdateLabel()
		{
			if (m_TextComponent != null && m_TextComponent.font != null)
				m_TextComponent.text = m_Text;
		}


		/// <summary>
		/// Open a keyboard for this input field
		/// </summary>
		public virtual void OpenKeyboard()
		{
			if (m_KeyboardOpen)
				return;

			m_KeyboardOpen = true;

			m_Keyboard = spawnKeyboard();

			m_Keyboard.gameObject.SetActive(true);

			this.StopCoroutine(ref m_MoveKeyboardCoroutine);

			var keyboardOutOfRange = (m_Keyboard.transform.position - transform.position).magnitude > 0.25f;
			m_MoveKeyboardCoroutine = StartCoroutine(MoveKeyboardToInputField(keyboardOutOfRange));
		}

		IEnumerator MoveKeyboardToInputField(bool instant)
		{
			const float kKeyboardYOffset = 0.05f;
			var targetPosition = transform.position + Vector3.up * kKeyboardYOffset;

			if (!instant && !m_Keyboard.collapsed)
			{
				var t = 0f;
				while (t < kMoveKeyboardTime)
				{
					m_Keyboard.transform.position = Vector3.Lerp(m_Keyboard.transform.position, targetPosition, t / kMoveKeyboardTime);
					m_Keyboard.transform.rotation = Quaternion.LookRotation(transform.position - U.Camera.GetMainCamera().transform.position);
					t += Time.unscaledDeltaTime;
					yield return null;
				}
			}

			m_Keyboard.transform.position = targetPosition;
			m_Keyboard.transform.rotation = Quaternion.LookRotation(transform.position - U.Camera.GetMainCamera().transform.position);
			m_MoveKeyboardCoroutine = null;

			m_Keyboard.Setup(OnKeyPress);
		}

		/// <summary>
		/// Close the keyboard and optionally run a collapse animation
		/// </summary>
		/// <param name="collapse">Should animate collapse?</param>
		/// <returns>If a keyboard was closed</returns>
		public virtual bool CloseKeyboard(bool collapse = false)
		{
			if (m_Keyboard == null || !m_KeyboardOpen)
				return false;

			m_KeyboardOpen = false;

			this.StopCoroutine(ref m_MoveKeyboardCoroutine);

			if (collapse)
				m_Keyboard.Collapse(FinalizeClose);
			else
				FinalizeClose();

			return true;
		}

		void FinalizeClose()
		{
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
			CloseKeyboard(true);
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
