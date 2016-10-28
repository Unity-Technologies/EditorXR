using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.UI
{
	public abstract class InputField : Selectable, IPointerClickHandler
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

		private bool m_Open;

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

		public void OnPointerClick(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			{
				if (rayEventData != null)
				{
					if (m_Open)
						Close();
					else
						Open();
				}
				else if (m_Open)
					Close();
			}
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

		public virtual void Open()
		{
			if (m_Open) return;
			m_Open = true;

			m_Keyboard = spawnKeyboard();


//			if (m_WaitThenOpenCoroutine != null)
//				StopCoroutine(m_WaitThenOpenCoroutine);
//			m_WaitThenOpenCoroutine = StartCoroutine(WaitThenOpen());
//		}
//
//		IEnumerator WaitThenOpen()
//		{
//			while (m_Keyboard != null && m_Keyboard.collapsing)
//			{
//				yield return null;
//			}

			m_Keyboard.gameObject.SetActive(true);

			m_Keyboard.transform.position = transform.position + Vector3.up * 0.05f;
			var rotation = Quaternion.LookRotation(transform.position - U.Camera.GetMainCamera().transform.position);
			m_Keyboard.transform.rotation = rotation;
			m_Keyboard.Setup(OnKeyPress);

//			if (m_MoveKeyboardCoroutine != null)
//				StopCoroutine(m_MoveKeyboardCoroutine);
//			m_MoveKeyboardCoroutine = StartCoroutine(MoveKeyboardToInputField());

		}

		IEnumerator MoveKeyboardToInputField()
		{
			var targetPosition = transform.position + Vector3.up * 0.05f;
			var rotation = Quaternion.LookRotation(transform.position - U.Camera.GetMainCamera().transform.position);

			var t = 0f;
			while (t < kMoveKeyboardTime)
			{
				m_Keyboard.transform.position = Vector3.Lerp(m_Keyboard.transform.position, targetPosition, t / kMoveKeyboardTime);
				t += Time.unscaledDeltaTime;
				yield return null;
			}
			m_Keyboard.transform.position = targetPosition;
			m_Keyboard.transform.rotation = rotation;
			m_MoveKeyboardCoroutine = null;

			m_Keyboard.Setup(OnKeyPress);
		}

		public virtual void Close()
		{
			m_Open = false;

			if (m_Keyboard == null) return;

//			m_Keyboard.Collapse(FinalizeClose);
//		}
//
//		void FinalizeClose()
//		{
//			m_Open = false;
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
