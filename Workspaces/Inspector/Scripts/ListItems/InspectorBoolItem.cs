using UnityEngine;
using UnityEngine.UI;

public class InspectorBoolItem : InspectorPropertyItem
{
	[SerializeField]
	private Toggle m_Toggle;

	private bool m_Setup;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		if (!m_Setup)
		{
			m_Setup = true;
			m_Toggle.onValueChanged.AddListener(SetValue);
		}

		m_Toggle.isOn = m_SerializedProperty.boolValue;
	}

	private void SetValue(bool value)
	{
		if (m_SerializedProperty.boolValue != value)
		{
			m_SerializedProperty.boolValue = value;
			data.serializedObject.ApplyModifiedProperties();
		}
	}
}