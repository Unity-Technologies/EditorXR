using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Utilities;

public class InspectorUnimplementedItem : InspectorPropertyItem
{
	[SerializeField]
	Text m_TypeLabel;

#if UNITY_EDITOR
	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_TypeLabel.text = U.Object.NicifySerializedPropertyType(m_SerializedProperty.type);
	}
#endif
}