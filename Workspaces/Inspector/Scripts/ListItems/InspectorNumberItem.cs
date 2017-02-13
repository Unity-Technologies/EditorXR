using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Data;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Workspaces;

public class InspectorNumberItem : InspectorPropertyItem
{
	[SerializeField]
	NumericInputField m_InputField;

	[SerializeField]
	WorkspaceButton[] m_IncrementDecrementButtons;

#if UNITY_EDITOR
	public SerializedPropertyType propertyType { get; private set; }
	public event Action<PropertyData> arraySizeChanged;

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
		// Do not increment undo group because NumericInputField does it for us
		if (SetValueIfPossible(input))
		{
			blockUndoPostProcess(); // Undo is registered by ApplyModifiedProperties
			data.serializedObject.ApplyModifiedProperties();
		}
	}

	bool SetValueIfPossible(string input)
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
					if (arraySizeChanged != null)
						arraySizeChanged((PropertyData)data);

					return true;
				}
				break;
			case SerializedPropertyType.Integer:
				int i;
				if (int.TryParse(input, out i) && m_SerializedProperty.intValue != i)
				{
					m_SerializedProperty.intValue = i;

					m_InputField.text = i.ToString();
					m_InputField.ForceUpdateLabel();

					return true;
				}
				break;
			case SerializedPropertyType.Float:
				float f;
				if (float.TryParse(input, out f) && !Mathf.Approximately(m_SerializedProperty.floatValue, f))
				{
					m_SerializedProperty.floatValue = f;

					m_InputField.text = f.ToString();
					m_InputField.ForceUpdateLabel();

					return true;
				}
				break;
		}

		return false;
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
		if (SetValueIfPossible(dropObject.ToString()))
			FinalizeModifications();
	}

	protected override void OnDragging(BaseHandle baseHandle, HandleEventData eventData)
	{
		base.OnDragging(baseHandle, eventData);

		if (m_ClickedField && m_ClickCount == 0)
		{
			foreach (var button in m_IncrementDecrementButtons)
				button.alternateIconVisible = true;
		}
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

			foreach (var button in m_IncrementDecrementButtons)
				button.alternateIconVisible = false;
		}
	}

	public void Increment()
	{
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.ArraySize:
			case SerializedPropertyType.Integer:
				if (SetValueIfPossible((m_SerializedProperty.intValue + 1).ToString()))
					FinalizeModifications();
				break;
			case SerializedPropertyType.Float:
				if (SetValueIfPossible((m_SerializedProperty.floatValue + 1).ToString()))
					FinalizeModifications();
				break;
		}
	}

	public void Decrement()
	{
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.ArraySize:
			case SerializedPropertyType.Integer:
				if (SetValueIfPossible((m_SerializedProperty.intValue - 1).ToString()))
					FinalizeModifications();
				break;
			case SerializedPropertyType.Float:
				if (SetValueIfPossible((m_SerializedProperty.floatValue - 1).ToString()))
					FinalizeModifications();
				break;
		}
	}
#endif
}