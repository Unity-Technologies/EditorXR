
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorBoolItem : InspectorPropertyItem
    {
        [SerializeField]
        Toggle m_Toggle;

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

            m_Toggle.isOn = m_SerializedProperty.boolValue;
        }

        protected override void FirstTimeSetup()
        {
            base.FirstTimeSetup();

            m_Toggle.onValueChanged.AddListener(SetValue);
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();
            m_Toggle.isOn = m_SerializedProperty.boolValue;
        }

        public void SetValue(bool value)
        {
            if (m_SerializedProperty.boolValue != value)
            {
                m_SerializedProperty.boolValue = value;

                FinalizeModifications();
            }
        }
    }
}

