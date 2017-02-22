using UnityEngine;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine.UI;
using UnityEditor.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	internal sealed class InspectorUnimplementedItem : InspectorPropertyItem
	{
		[SerializeField]
		Text m_TypeLabel;

#if UNITY_EDITOR
		public override void Setup(InspectorData data)
		{
			base.Setup(data);

			m_TypeLabel.text = ObjectUtils.NicifySerializedPropertyType(m_SerializedProperty.type);
		}
#endif
	}
}