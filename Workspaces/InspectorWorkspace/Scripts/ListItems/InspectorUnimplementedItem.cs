
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorUnimplementedItem : InspectorPropertyItem
    {
        [SerializeField]
        Text m_TypeLabel;

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

            m_TypeLabel.text = ObjectUtils.NicifySerializedPropertyType(m_SerializedProperty.type);
        }
    }
}

