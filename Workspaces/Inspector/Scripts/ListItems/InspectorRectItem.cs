using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.UI;

public class InspectorRectItem : InspectorPropertyItem
{
	[SerializeField]
	NumericInputField[] m_CenterFields;

	[SerializeField]
	NumericInputField[] m_SizeFields;

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

		//TODO: Expose onValueChanged in Inspector
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
		if (!float.TryParse(input, out value)) return false;

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

	protected override object GetDropObject(Transform fieldBlock)
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

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		return droppedObject is string || droppedObject is Rect || droppedObject is Vector2 || droppedObject is Vector3 || droppedObject is Vector4;
	}

	public override bool ReceiveDrop(GameObject target, object droppedObject)
	{
		if (!TestDrop(target, droppedObject))
			return false;

		var str = droppedObject as string;
		if (str != null)
		{
			var targetParent = target.transform.parent;
			var inputField = targetParent.GetComponentInChildren<NumericInputField>();
			var index = Array.IndexOf(m_SizeFields, inputField);
			if (index > -1 && SetValue(str, index))
			{
				inputField.text = str;
				inputField.ForceUpdateLabel();
				return true;
			}

			index = Array.IndexOf(m_CenterFields, inputField);
			if (index > -1 && SetValue(str, index, true))
			{
				inputField.text = str;
				inputField.ForceUpdateLabel();
				return true;
			}

			return false;
		}

		if (droppedObject is Rect)
		{
			m_SerializedProperty.rectValue = (Rect)droppedObject;

			UpdateInputFields(m_SerializedProperty.rectValue);

			data.serializedObject.ApplyModifiedProperties();
			return true;
		}

		if (droppedObject is Vector2 || droppedObject is Vector3 || droppedObject is Vector4)
		{
			var vector2 = (Vector2)droppedObject;
			var targetParent = target.transform.parent;
			var inputField = targetParent.GetComponentInChildren<NumericInputField>();
			var rect = m_SerializedProperty.rectValue;

			if (m_CenterFields.Contains(inputField))
				rect.center = vector2;
			else
				rect.size = vector2;

			m_SerializedProperty.rectValue = rect;

			UpdateInputFields(rect);

			data.serializedObject.ApplyModifiedProperties();
			
			return true;
		}

		return false;
	}
}