
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorStringItem : InspectorPropertyItem
    {
        [SerializeField]
        StandardInputField m_InputField;

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

            UpdateInputField();
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();
            UpdateInputField();
        }

        void UpdateInputField()
        {
            base.Setup(data);

            var val = string.Empty;

#if UNITY_EDITOR
            switch (m_SerializedProperty.propertyType)
            {
                case SerializedPropertyType.String:
                    val = m_SerializedProperty.stringValue;
                    break;
                case SerializedPropertyType.Character:
                    val = m_SerializedProperty.intValue.ToString();
                    break;
            }
#endif

            m_InputField.text = val;
            m_InputField.ForceUpdateLabel();
        }

        public void SetValue(string input)
        {
            if (SetValueIfPossible(input))
                FinalizeModifications();
        }

        bool SetValueIfPossible(string input)
        {
#if UNITY_EDITOR
            switch (m_SerializedProperty.propertyType)
            {
                case SerializedPropertyType.String:
                    if (!m_SerializedProperty.stringValue.Equals(input))
                    {
                        m_SerializedProperty.stringValue = input;

                        m_InputField.text = input;
                        m_InputField.ForceUpdateLabel();

                        return true;
                    }
                    break;
                case SerializedPropertyType.Character:
                    char c;
                    if (char.TryParse(input, out c) && c != m_SerializedProperty.intValue)
                    {
                        m_SerializedProperty.intValue = c;

                        m_InputField.text = input;
                        m_InputField.ForceUpdateLabel();

                        return true;
                    }
                    break;
            }
#endif

            return false;
        }

        protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
        {
            return m_InputField.text;
        }

        protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            return dropObject is string;
        }

        protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            if (SetValueIfPossible(dropObject.ToString()))
                FinalizeModifications();
        }
    }
}

