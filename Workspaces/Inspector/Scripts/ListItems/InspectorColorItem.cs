using System;
using UnityEngine;
using UnityEngine.VR.UI;

public class InspectorColorItem : InspectorPropertyItem
{
	[SerializeField]
	NumericInputField[] m_InputFields;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		UpdateInputFields(m_SerializedProperty.colorValue);
	}

	void UpdateInputFields(Color color)
	{
		for (var i = 0; i < 4; i++)
		{
			m_InputFields[i].text = color[i].ToString();
			m_InputFields[i].ForceUpdateLabel();
		}
	}

	protected override void FirstTimeSetup()
	{
		base.FirstTimeSetup();

		//TODO: expose onValueChanged in Inspector
		for (var i = 0; i < m_InputFields.Length; i++)
		{
			var index = i;
			m_InputFields[i].onValueChanged.AddListener(value => SetValue(value, index));
		}
	}

	public bool SetValue(string input, int index)
	{
		float value;
		if (!float.TryParse(input, out value)) return false;

		var color = m_SerializedProperty.colorValue;
		if (!Mathf.Approximately(color[index], value))
		{
			color[index] = value;
			m_SerializedProperty.colorValue = color;

			UpdateInputFields(color);

			data.serializedObject.ApplyModifiedProperties();

			return true;
		}

		return false;
	}

	protected override object GetDropObject(Transform fieldBlock)
	{
		object dropObject = null;
		var inputfields = fieldBlock.GetComponentsInChildren<NumericInputField>();
		if (inputfields.Length > 1)
		{
			dropObject = m_SerializedProperty.colorValue;
		} else if (inputfields.Length > 0)
			dropObject = inputfields[0].text;

		return dropObject;
	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		return droppedObject is string || droppedObject is Vector2 || droppedObject is Vector3 || droppedObject is Vector4 || droppedObject is Quaternion || droppedObject is Color;
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
			var index = Array.IndexOf(m_InputFields, inputField);

			if (SetValue(str, index))
			{
				inputField.text = str;
				inputField.ForceUpdateLabel();
				return true;
			}
			return false;
		}

		if (droppedObject is Color)
		{
			m_SerializedProperty.colorValue = (Color)droppedObject;

			UpdateInputFields(m_SerializedProperty.colorValue);

			data.serializedObject.ApplyModifiedProperties();
			return true;
		}

		var color = m_SerializedProperty.colorValue;
		if (droppedObject is Vector2)
		{
			var vector2 = (Vector2) droppedObject;
			color.r = vector2.x;
			color.g = vector2.y;
			m_SerializedProperty.colorValue = color;

			UpdateInputFields(color);

			data.serializedObject.ApplyModifiedProperties();
			return true;
		}

		if (droppedObject is Vector3)
		{
			var vector3= (Vector3)droppedObject;
			color.r = vector3.x;
			color.g = vector3.y;
			color.b = vector3.z;
			m_SerializedProperty.colorValue = color;

			UpdateInputFields(color);

			data.serializedObject.ApplyModifiedProperties();
			return true;
		}

		if (droppedObject is Vector4)
		{
			var vector4 = (Vector4)droppedObject;
			color.r = vector4.x;
			color.g = vector4.y;
			color.b = vector4.z;
			color.a = vector4.w;
			m_SerializedProperty.colorValue = color;

			UpdateInputFields(color);

			data.serializedObject.ApplyModifiedProperties();
			return true;
		}

		return false;
	}
}