using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.UI;

public class InspectorRectItem : InspectorPropertyItem
{
	[SerializeField]
	NumericInputField[] m_CenterFields;

	[SerializeField]
	NumericInputField[] m_SizeFields;

#if UNITY_EDITOR
	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		UpdateInputFields(m_SerializedProperty.rectValue);
	}

	void UpdateInputFields(Rect rect)
	{
		for (var i = 0; i < m_CenterFields.Length; i++)
		{
			m_CenterFields[i].text = rect.center[i].ToString();
			m_CenterFields[i].ForceUpdateLabel();
			m_SizeFields[i].text = rect.size[i].ToString();
			m_SizeFields[i].ForceUpdateLabel();
		}
	}

	protected override void FirstTimeSetup()
	{
		base.FirstTimeSetup();

		for (var i = 0; i < m_CenterFields.Length; i++)
		{
			var index = i;
			m_CenterFields[i].onValueChanged.AddListener(value => SetValue(value, index, true));
			m_SizeFields[i].onValueChanged.AddListener(value => SetValue(value, index));
		}
	}

	bool SetValue(string input, int index, bool center = false)
	{
		float value;
		if (!float.TryParse(input, out value))
			return false;

		var rect = m_SerializedProperty.rectValue;
		var vector = center ? rect.center : rect.size;

		if (!Mathf.Approximately(vector[index], value))
		{
			vector[index] = value;
			if (center)
				rect.center = vector;
			else
				rect.size = vector;

			UpdateInputFields(rect);

			m_SerializedProperty.rectValue = rect;
			data.serializedObject.ApplyModifiedProperties();
		}

		return true;
	}

	protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
	{
		object dropObject = null;
		var inputfields = fieldBlock.GetComponentsInChildren<NumericInputField>();

		if (inputfields.Length > 3) // If we've grabbed all of the fields
			dropObject = m_SerializedProperty.rectValue;
		if (inputfields.Length > 1) // If we've grabbed one vector
		{
			if (m_CenterFields.Intersect(inputfields).Any())
				dropObject = m_SerializedProperty.rectValue.center;
			else
				dropObject = m_SerializedProperty.rectValue.size;
		} else if (inputfields.Length > 0) // If we've grabbed a single field
			dropObject = inputfields[0].text;

		return dropObject;
	}

	protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		return dropObject is string || dropObject is Rect || dropObject is Vector2
			|| dropObject is Vector3 || dropObject is Vector4;
	}

	protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		var str = dropObject as string;
		if (str != null)
		{
			var inputField = fieldBlock.GetComponentInChildren<NumericInputField>();
			var index = Array.IndexOf(m_SizeFields, inputField);
			if (index > -1 && SetValue(str, index))
			{
				inputField.text = str;
				inputField.ForceUpdateLabel();
			}

			index = Array.IndexOf(m_CenterFields, inputField);
			if (index > -1 && SetValue(str, index, true))
			{
				inputField.text = str;
				inputField.ForceUpdateLabel();
			}
		}

		if (dropObject is Rect)
		{
			m_SerializedProperty.rectValue = (Rect)dropObject;

			UpdateInputFields(m_SerializedProperty.rectValue);

			data.serializedObject.ApplyModifiedProperties();
		}

		if (dropObject is Vector2 || dropObject is Vector3 || dropObject is Vector4)
		{
			var vector2 = (Vector2)dropObject;
			var inputField = fieldBlock.GetComponentInChildren<NumericInputField>();
			var rect = m_SerializedProperty.rectValue;

			if (m_CenterFields.Contains(inputField))
				rect.center = vector2;
			else
				rect.size = vector2;

			m_SerializedProperty.rectValue = rect;

			UpdateInputFields(rect);

			data.serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}