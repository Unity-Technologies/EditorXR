using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.UI;

public class InspectorNumberItem : InspectorPropertyItem
{
	[SerializeField]
	private NumericInputField m_InputField;

	public SerializedPropertyType propertyType { get; private set; }

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		propertyType = m_SerializedProperty.propertyType;

		var val = string.Empty;
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.ArraySize:
			case SerializedPropertyType.Integer:
				val = m_SerializedProperty.intValue.ToString();
				m_InputField.numberType = NumericInputField.NumberType.Int;
				break;
			case SerializedPropertyType.Float:
				val = m_SerializedProperty.floatValue.ToString();
				m_InputField.numberType = NumericInputField.NumberType.Float;
				break;
		}

		m_InputField.text = val;
		m_InputField.ForceUpdateLabel();
	}

	public void SetValue(string input)
	{
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.ArraySize:
				int size;
				if (int.TryParse(input, out size) && m_SerializedProperty.intValue != size)
				{
					m_SerializedProperty.arraySize = size;

					m_InputField.text = size.ToString();
					m_InputField.ForceUpdateLabel();
					((PropertyData) data).updateParent();

					data.serializedObject.ApplyModifiedProperties();
				}
				break;
			case SerializedPropertyType.Integer:
				int i;
				if (int.TryParse(input, out i) && m_SerializedProperty.intValue != i)
				{
					m_SerializedProperty.intValue = i;

					m_InputField.text = i.ToString();
					m_InputField.ForceUpdateLabel();

					data.serializedObject.ApplyModifiedProperties();
				}
				break;
			case SerializedPropertyType.Float:
				float f;
				if (float.TryParse(input, out f) && !Mathf.Approximately(m_SerializedProperty.floatValue, f))
				{
					m_SerializedProperty.floatValue = f;

					m_InputField.text = f.ToString();
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

	protected override void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		base.OnDragEnded(baseHandle, eventData);
		// Update field value in case drag value was invalid (i.e. array size < 0)
		if (m_ClickedField)
		{
			var numericField = m_ClickedField as NumericInputField;
			if (numericField)
			{
				switch (m_SerializedProperty.propertyType)
				{
					case SerializedPropertyType.ArraySize:
					case SerializedPropertyType.Integer:
						numericField.text = m_SerializedProperty.intValue.ToString();
						numericField.ForceUpdateLabel();
						break;
					case SerializedPropertyType.Float:
						numericField.text = m_SerializedProperty.floatValue.ToString();
						numericField.ForceUpdateLabel();
						break;
				}
			}
		}
	}

	public void Increment()
	{
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.ArraySize:
			case SerializedPropertyType.Integer:
				SetValue((m_SerializedProperty.intValue + 1).ToString());
				break;
			case SerializedPropertyType.Float:
				SetValue((m_SerializedProperty.floatValue + 1).ToString());
				break;

		}
	}

	public void Decrement()
	{
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.ArraySize:
			case SerializedPropertyType.Integer:
				SetValue((m_SerializedProperty.intValue - 1).ToString());
				break;
			case SerializedPropertyType.Float:
				SetValue((m_SerializedProperty.floatValue - 1).ToString());
				break;

		}
	}
}