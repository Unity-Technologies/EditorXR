using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.UI
{
    abstract class InputField : Selectable, ISelectionFlags, IUsesViewerScale, IAllWorkspaces
    {
        public SelectionFlags selectionFlags
        {
            get { return m_SelectionFlags; }
            set { m_SelectionFlags = value; }
        }

        [Serializable]
        public class OnChangeEvent : UnityEvent<string>
        {
        }

        const float k_MoveKeyboardTime = 0.2f;

#pragma warning disable 649
        [SerializeField]
        [FlagsProperty]
        SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

        [SerializeField]
        OnChangeEvent m_OnValueChanged = new OnChangeEvent();

        [SerializeField]
        TextMeshProUGUI m_TextComponent;

        [SerializeField]
        int m_CharacterLimit = 10;

        [HideInInspector]
        [SerializeField] // Serialized so that this remains set after cloning
        protected string m_Text = string.Empty;
#pragma warning restore 649

        bool m_KeyboardOpen;

        Coroutine m_MoveKeyboardCoroutine;

        protected KeyboardUI m_Keyboard;

        public Func<KeyboardUI> spawnKeyboard { private get; set; }

        public OnChangeEvent onValueChanged { get { return m_OnValueChanged; } }

        public virtual string text
        {
            get { return m_Text; }
            set
            {
                if (m_Text == value)
                    return;

                if (value == null)
                    value = "";

                m_Text = m_CharacterLimit > 0 && value.Length > m_CharacterLimit ? value.Substring(0, m_CharacterLimit) : value;
            }
        }

        public List<IWorkspace> allWorkspaces { private get; set; }

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

        protected override void OnDisable()
        {
            // hide the keyboard if there are 0 open inspectors or the selection is null
            if (m_KeyboardOpen && (Selection.activeObject == null || !FindAnyOpenInspector()))
                CloseKeyboard(true);
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
        /// Check if any Inspector workspaces are still open
        /// </summary>
        protected bool FindAnyOpenInspector()
        {
            if (allWorkspaces == null || allWorkspaces.Count == 0)
                return false;

            var found = false;
            foreach (var w in allWorkspaces)
            {
                if (w is IInspectorWorkspace)
                {
                    found = true;
                    break;
                }
            }

            return found;
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

#if UNITY_EDITOR
            Undo.IncrementCurrentGroup(); // Every time we open the keyboard is a new modification
#endif
        }

        IEnumerator MoveKeyboardToInputField(bool instant)
        {
            const float kKeyboardYOffset = 0.05f;
            var targetPosition = transform.position + Vector3.up * kKeyboardYOffset * this.GetViewerScale();

            if (!instant && !m_Keyboard.collapsed)
            {
                var t = 0f;
                while (t < k_MoveKeyboardTime)
                {
                    m_Keyboard.transform.position = Vector3.Lerp(m_Keyboard.transform.position, targetPosition, t / k_MoveKeyboardTime);
                    m_Keyboard.transform.rotation = Quaternion.LookRotation(transform.position - CameraUtils.GetMainCamera().transform.position);
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            m_Keyboard.transform.position = targetPosition;
            m_Keyboard.transform.rotation = Quaternion.LookRotation(transform.position - CameraUtils.GetMainCamera().transform.position);
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
