using System;
using TMPro;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    abstract class InspectorPropertyItem : InspectorListItem
    {
#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_Label;

        [SerializeField]
        Transform m_TooltipTarget;

        [SerializeField]
        Transform m_TooltipSource;
#pragma warning restore 649

        public Transform tooltipTarget
        {
            get { return m_TooltipTarget; }
        }

        public Transform tooltipSource
        {
            get { return m_TooltipSource; }
        }

        public TextAlignment tooltipAlignment
        {
            get { return TextAlignment.Right; }
        }

        public Action<ITooltip> showTooltip { get; set; }
        public Action<ITooltip> hideTooltip { get; set; }

        public string tooltipText
        {
#if UNITY_EDITOR
            get { return m_SerializedProperty.tooltip; }
#else
            get { return string.Empty; }
#endif
        }

        protected SerializedProperty m_SerializedProperty;

        public override void Setup(InspectorData data, bool firstTime = false)
        {
            base.Setup(data, firstTime);

            m_SerializedProperty = ((PropertyData)data).property;

#if UNITY_EDITOR
            m_Label.text = m_SerializedProperty.displayName;
#endif
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();

#if UNITY_EDITOR
            m_SerializedProperty = data.serializedObject.FindProperty(m_SerializedProperty.propertyPath);
#endif
        }

        protected void FinalizeModifications()
        {
#if UNITY_EDITOR
            Undo.IncrementCurrentGroup();
            data.serializedObject.ApplyModifiedProperties();
#endif
        }
    }
}
