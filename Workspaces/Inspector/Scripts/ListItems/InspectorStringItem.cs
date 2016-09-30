using UnityEditor;
using UnityEngine;

public class InspectorStringItem : InspectorPropertyItem
{
	[SerializeField]
	private StandardInputField m_InputField;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var val = string.Empty;
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.String:
				val = m_SerializedProperty.stringValue;
				break;
			case SerializedPropertyType.Character:
				val = m_SerializedProperty.intValue.ToString();
				break;
		}

		SetValue(val);
	}

	public void SetValue(string input)
	{
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.String:
				if (!m_SerializedProperty.stringValue.Equals(input))
				{
					m_SerializedProperty.stringValue = input;

					m_InputField.text = input;
					m_InputField.ForceUpdateLabel();

					data.serializedObject.ApplyModifiedProperties();
				}
				break;
			case SerializedPropertyType.Character:
				char c;
				if (char.TryParse(input, out c) && c != m_SerializedProperty.intValue)
				{
					m_SerializedProperty.intValue = c;

					m_InputField.text = input;
					m_InputField.ForceUpdateLabel();

					data.serializedObject.ApplyModifiedProperties();
				}
				break;
		}
	}

	protected override object GetDropObject(Transform fieldBlock)
	{
		return m_InputField.text;
	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		return droppedObject is string;
	}

	public override bool RecieveDrop(GameObject target, object droppedObject)
	{
		SetValue(droppedObject.ToString());
		return true;
	}
}