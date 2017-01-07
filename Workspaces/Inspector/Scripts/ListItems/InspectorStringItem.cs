using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.UI;

public class InspectorStringItem : InspectorPropertyItem
{
	[SerializeField]
	StandardInputField m_InputField;

#if UNITY_EDITOR
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

	protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
	{
		return m_InputField.text;
	}

	protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		return dropObject is string;
	}

	protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		SetValue(dropObject.ToString());
	}
#endif
}