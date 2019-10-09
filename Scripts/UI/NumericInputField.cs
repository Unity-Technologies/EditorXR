using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class NumericInputField : InputField
    {
        public enum NumberType
        {
            Float,
            Int,
        }

        const string k_FloatFieldFormatString = "g7";
        const string k_IntFieldFormatString = "#######0";
        const float k_FloatDragSensitivity = 2f;
        const float k_IntDragSensitivity = 10f;
        const string k_AllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()";
        const string k_AllowedCharactersForInt = "0123456789-*/+%^()";
        const string k_OperandCharacters = "-*/+%^()";

        public NumberType numberType
        {
            get { return m_NumberType; }
            set { m_NumberType = value; }
        }

        [SerializeField]
        NumberType m_NumberType = NumberType.Float;

        bool m_Dragging;
        Vector3 m_LastPointerPosition;
        int m_OperandCount;

        public override string text
        {
            get { return base.text; }
            set
            {
                if (m_Dragging)
                    return;

                base.text = value;
            }
        }

        public void BeginSliderDrag(Transform rayOrigin)
        {
            ParseNumberField();
            m_LastPointerPosition = GetLocalPointerPosition(rayOrigin);
            m_Dragging = true;
#if UNITY_EDITOR
            Undo.IncrementCurrentGroup(); // Every drag start is a new modification
#endif
        }

        public void EndSliderDrag(Transform rayOrigin)
        {
            m_Dragging = false;
            m_LastPointerPosition = GetLocalPointerPosition(rayOrigin);
        }

        public void SliderDrag(Transform rayOrigin)
        {
            var delta = GetLocalPointerPosition(rayOrigin) - m_LastPointerPosition;

            if (m_NumberType == NumberType.Float)
            {
                float num;
                if (!float.TryParse(text, out num))
                    num = 0f;

                const float maxDelta = 0.1f;
                var deltaX = Mathf.Clamp(delta.x, -maxDelta, maxDelta);
                var dragSensitivity = CalculateDragSensitivity(num) * k_FloatDragSensitivity;
                num += deltaX * dragSensitivity;
                m_Text = num.ToString(k_FloatFieldFormatString);
                m_LastPointerPosition = GetLocalPointerPosition(rayOrigin);
            }
            else
            {
                int intNum;
                if (!int.TryParse(text, out intNum))
                    intNum = 0;

                var dragSensitivity = CalculateDragSensitivity(intNum) * k_IntDragSensitivity;
                var change = (int)(delta.x * dragSensitivity);

                intNum += change;

                m_Text = intNum.ToString(k_IntFieldFormatString);
                if (change != 0)
                    m_LastPointerPosition = GetLocalPointerPosition(rayOrigin);
            }

            SendOnValueChangedAndUpdateLabel();
        }

        Vector3 GetLocalPointerPosition(Transform rayOrigin)
        {
            Vector3 hitPos;
            MathUtilsExt.LinePlaneIntersection(out hitPos, rayOrigin.position, rayOrigin.forward, -transform.forward,
                transform.position);

            return transform.InverseTransformPoint(hitPos);
        }

        protected override void UpdateLabel()
        {
            base.UpdateLabel();

            if (m_Keyboard != null)
                UpdateHandleButtonText();
        }

        public override void OpenKeyboard()
        {
            base.OpenKeyboard();

            UpdateHandleButtonText();
        }

        void UpdateHandleButtonText()
        {
            if (IsExpression())
            {
                m_Keyboard.DeactivateShiftModeOnKey(m_Keyboard.handleButton);
                m_Keyboard.handleButton.textComponent.text = "=";
            }
            else
            {
                m_Keyboard.ActivateShiftModeOnKey(m_Keyboard.handleButton);
                m_Keyboard.handleButton.textComponent.text = "x";
            }
        }

        public override bool CloseKeyboard(bool collapse = false)
        {
            ParseNumberField();

            return base.CloseKeyboard(collapse);
        }

        protected override bool IsValid(char ch)
        {
            if (!base.IsValid(ch))
                return false;

            switch (m_NumberType)
            {
                case NumberType.Float:
                    return k_AllowedCharactersForFloat.Contains(ch.ToString());
                case NumberType.Int:
                    return k_AllowedCharactersForInt.Contains(ch.ToString());
                default:
                    return false;
            }
        }

        protected override void Append(char c)
        {
            var len = m_Text.Length;
            text += c;
            if (len != m_Text.Length)
            {
                if (IsOperand(c))
                    m_OperandCount++;

                if (!IsExpression())
                    SendOnValueChangedAndUpdateLabel();
                else
                    UpdateLabel();
            }
        }

        protected override void Backspace()
        {
            if (m_Text.Length == 0) return;

            var ch = m_Text[m_Text.Length - 1];
            if (IsOperand(ch))
                m_OperandCount--;

            m_Text = m_Text.Remove(m_Text.Length - 1);

            if (!IsExpression())
                SendOnValueChangedAndUpdateLabel();
            else
                UpdateLabel();
        }

        protected override void Tab()
        {
            Return();
        }

        protected override void Clear()
        {
            base.Clear();
            m_OperandCount = 0;
        }

        protected override void Return()
        {
            ParseNumberField();
        }

        protected override void Space()
        {
            Return();
        }

        protected override void Shift() {}

        protected override void CapsLock() {}

        bool IsExpression()
        {
            return m_OperandCount > 0;
        }

        bool IsOperand(char c)
        {
            return k_OperandCharacters.Contains(c.ToString()) && !(m_Text.Length == 0 && c == '-');
        }

        static float CalculateDragSensitivity(float value)
        {
            if (float.IsInfinity(value) || float.IsNaN(value))
                return 0f;

            return Mathf.Max(1, Mathf.Pow(Mathf.Abs(value), 0.5f));
        }

        void ParseNumberField()
        {
            if (!IsExpression())
                return;

            var str = m_Text;

            if (m_NumberType == NumberType.Float)
            {
                float floatVal;

                // Make sure that comma & period are interchangeable.
                m_Text = m_Text.Replace(',', '.');

                if (!float.TryParse(m_Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out floatVal))
                {
#if UNITY_EDITOR
                    ExpressionEvaluator.Evaluate<float>(m_Text, out floatVal);
#endif
                }

                if (float.IsNaN(floatVal))
                    floatVal = 0;

                m_Text = floatVal.ToString(k_FloatFieldFormatString);
            }
            else
            {
                int intVal;
                if (!int.TryParse(m_Text, out intVal))
                {
#if UNITY_EDITOR
                    ExpressionEvaluator.Evaluate<int>(m_Text, out intVal);
                    m_Text = intVal.ToString(k_IntFieldFormatString);
#endif
                }
            }

            m_OperandCount = 0;

            if (str != m_Text)
                SendOnValueChangedAndUpdateLabel();
        }
    }
}