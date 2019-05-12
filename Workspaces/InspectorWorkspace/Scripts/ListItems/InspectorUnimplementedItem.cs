using TMPro;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorUnimplementedItem : InspectorPropertyItem
    {
#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_TypeLabel;
#pragma warning restore 649

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

#if UNITY_EDITOR
            m_TypeLabel.text = ObjectUtils.NicifySerializedPropertyType(m_SerializedProperty.type);
#endif
        }
    }
}
