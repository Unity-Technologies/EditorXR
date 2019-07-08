using TMPro;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorUnimplementedItem : InspectorPropertyItem
    {
#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_TypeLabel;
#pragma warning restore 649

        public override void Setup(InspectorData datum, bool firstTime = false)
        {
            base.Setup(datum, firstTime);

#if UNITY_EDITOR
            m_TypeLabel.text = EditorUtils.NicifySerializedPropertyType(m_SerializedProperty.type);
#endif
        }
    }
}
