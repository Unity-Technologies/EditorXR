using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorBoolItem : InspectorPropertyItem
    {
#pragma warning disable 649
        [SerializeField]
        Toggle m_Toggle;
#pragma warning restore 649

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

#if UNITY_EDITOR
            m_Toggle.isOn = m_SerializedProperty.boolValue;
#endif
        }

        protected override void FirstTimeSetup()
        {
            base.FirstTimeSetup();

            m_Toggle.onValueChanged.AddListener(SetValue);
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();
#if UNITY_EDITOR
            m_Toggle.isOn = m_SerializedProperty.boolValue;
#endif
        }

        public void SetValue(bool value)
        {
#if UNITY_EDITOR
            if (m_SerializedProperty.boolValue != value)
            {
                m_SerializedProperty.boolValue = value;

                FinalizeModifications();
            }
#endif
        }
    }
}
