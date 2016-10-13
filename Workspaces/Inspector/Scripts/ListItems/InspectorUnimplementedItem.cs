using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;

public class InspectorUnimplementedItem : InspectorPropertyItem
{
	[SerializeField]
	Text m_TypeLabel;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_TypeLabel.text = U.Object.NicifySerializedPropertyType(m_SerializedProperty.type);
	}
}