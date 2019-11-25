using Unity.Labs.EditorXR.Data;
using Unity.Labs.EditorXR.UI;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR.Workspaces
{
    sealed class InspectorStringItem : InspectorPropertyItem
    {
#pragma warning disable 649
        [SerializeField]
        StandardInputField m_InputField;
#pragma warning restore 649

        public override void Setup(InspectorData data, bool firstTime = false)
        {
            base.Setup(data, firstTime);

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
