
using System;
using TMPro;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    abstract class InspectorPropertyItem : InspectorListItem
    {
        [SerializeField]
        TextMeshProUGUI m_Label;

        public Transform tooltipTarget
        {
            get { return m_TooltipTarget; }
        }

        [SerializeField]
        Transform m_TooltipTarget;

        public Transform tooltipSource
        {
            get { return m_TooltipSource; }
        }

        [SerializeField]
        Transform m_TooltipSource;

        public TextAlignment tooltipAlignment
        {
            get { return TextAlignment.Right; }
        }

        public Action<ITooltip> showTooltip { get; set; }
        public Action<ITooltip> hideTooltip { get; set; }

        public string tooltipText
        {
            get { return m_SerializedProperty.tooltip; }
        }

        protected SerializedProperty m_SerializedProperty;

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

            m_SerializedProperty = ((PropertyData)data).property;

            m_Label.text = m_SerializedProperty.displayName;
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();

            m_SerializedProperty = data.serializedObject.FindProperty(m_SerializedProperty.propertyPath);
        }

        protected void FinalizeModifications()
        {
            Undo.IncrementCurrentGroup();
            data.serializedObject.ApplyModifiedProperties();
        }
    }
}

