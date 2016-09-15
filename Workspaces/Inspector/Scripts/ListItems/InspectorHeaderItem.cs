using UnityEngine;
using UnityEngine.UI;

public class InspectorHeaderItem : InspectorListItem
{
	[SerializeField]
	private Toggle m_ActiveToggle;

	[SerializeField]
	private InputField m_NameField;

	[SerializeField]
	private Toggle m_StaticToggle;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_NameField.text = data.serializedObject.targetObject.name;
	}
}