
using TMPro;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorUnimplementedItem : InspectorPropertyItem
    {
        [SerializeField]
        TextMeshProUGUI m_TypeLabel;

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

#if UNITY_EDITOR
            m_TypeLabel.text = ObjectUtils.NicifySerializedPropertyType(m_SerializedProperty.type);
#endif
        }
    }
}

