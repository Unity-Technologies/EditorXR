using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.UI;

public class InspectorBoundsItem : InspectorPropertyItem
{
	[SerializeField]
	NumericInputField[] m_CenterFields;

	[SerializeField]
	NumericInputField[] m_ExtentsFields;

#if UNITY_EDITOR
	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		UpdateInputFields(m_SerializedProperty.boundsValue);
	}

	void UpdateInputFields(Bounds bounds)
	{
		for (var i = 0; i < m_CenterFields.Length; i++)
		{
			m_CenterFields[i].text = bounds.center[i].ToString();
			m_ExtentsFields[i].text = bounds.extents[i].ToString();
		}
	}

	protected override void FirstTimeSetup()
	{
		base.FirstTimeSetup();

		for (var i = 0; i < m_CenterFields.Length; i++)
		{
			var index = i;
			m_CenterFields[i].onValueChanged.AddListener(value => SetValue(value, index, true));
			m_ExtentsFields[i].onValueChanged.AddListener(value => SetValue(value, index));
		}
	}

	bool SetValue(string input, int index, bool center = false)
	{
		float value;
		if (!float.TryParse(input, out value))
			return false;

		var bounds = m_SerializedProperty.boundsValue;
		var vector = center ? bounds.center : bounds.extents;

		if (!Mathf.Approximately(vector[index], value))
		{
			vector[index] = value;
			if (center)
				bounds.center = vector;
			else
				bounds.extents = vector;

			UpdateInputFields(bounds);

			m_SerializedProperty.boundsValue = bounds;
			data.serializedObject.ApplyModifiedProperties();

			return true;
		}

		return false;
	}

	protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
	{
		object dropObject = null;
		var inputfields = fieldBlock.GetComponentsInChildren<NumericInputField>();

		if (inputfields.Length > 3) // If we've grabbed all of the fields
			dropObject = m_SerializedProperty.boundsValue;
		if (inputfields.Length > 1) // If we've grabbed one vector
		{
			if (m_CenterFields.Intersect(inputfields).Any())
				dropObject = m_SerializedProperty.boundsValue.center;
			else
				dropObject = m_SerializedProperty.boundsValue.extents;
		}
		else if (inputfields.Length > 0) // If we've grabbed a single field
			dropObject = inputfields[0].text;

		return dropObject;
	}

	protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		return dropObject is string || dropObject is Bounds;
	}

	protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		var str = dropObject as string;
		if (str != null)
		{
			var inputField = fieldBlock.GetComponentInChildren<NumericInputField>();
			var index = Array.IndexOf(m_ExtentsFields, inputField);
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

		if (dropObject is Bounds)
		{
			m_SerializedProperty.boundsValue = (Bounds)dropObject;

			UpdateInputFields(m_SerializedProperty.boundsValue);

			data.serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}