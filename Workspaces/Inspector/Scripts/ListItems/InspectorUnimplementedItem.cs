using UnityEngine;
using UnityEngine.UI;

public class InspectorUnimplementedItem : InspectorPropertyItem
{
	[SerializeField]
	private Text m_TypeLabel;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_TypeLabel.text = m_SerializedProperty.type;
	}
}