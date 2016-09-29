using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.Modules;

public class InspectorRectItem : InspectorPropertyItem
{
	[SerializeField]
	private NumericInputField[] m_CenterFields;
	[SerializeField]
	private NumericInputField[] m_SizeFields;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		UpdateInputFields(m_SerializedProperty.rectValue);
	}

	private void UpdateInputFields(Rect rect)
	{
		for (int i = 0; i < m_CenterFields.Length; i++)
		{
			m_CenterFields[i].text = rect.center[i].ToString();
			m_SizeFields[i].text = rect.size[i].ToString();
		}
	}

	protected override void FirstTimeSetup()
	{
		base.FirstTimeSetup();

		//TODO: Expose onValueChanged in Inspector
		for (int i = 0; i < m_CenterFields.Length; i++)
		{
			var index = i;
			m_CenterFields[i].onValueChanged.AddListener(value => SetValue(value, index, true));
			m_SizeFields[i].onValueChanged.AddListener(value => SetValue(value, index));
		}
	}

	private bool SetValue(string input, int index, bool center = false)
	{
		float value;
		if (!float.TryParse(input, out value)) return false;

		var bounds = m_SerializedProperty.boundsValue;
		var vector = center ? bounds.center : bounds.extents;

		if (!Mathf.Approximately(vector[index], value))
		{
			vector[index] = value;
			if (center)
				bounds.center = vector;
			else
				bounds.extents = vector;
			m_SerializedProperty.boundsValue = bounds;
			data.serializedObject.ApplyModifiedProperties();
		}

		return true;
	}

	protected override void DropItem(Transform fieldBlock, IDropReciever dropReciever, GameObject target)
	{
		object droppedObject = null;
		var inputfields = fieldBlock.GetComponentsInChildren<NumericInputField>();

		if (inputfields.Length > 3) // If we've grabbed all of the fields
			droppedObject = m_SerializedProperty.boundsValue;
		if (inputfields.Length > 1) // If we've grabbed one vector
		{
			if (m_CenterFields.Intersect(inputfields).Any())
				droppedObject = m_SerializedProperty.boundsValue.center;
			else
				droppedObject = m_SerializedProperty.boundsValue.extents;
		} else if (inputfields.Length > 0) // If we've grabbed a single field
			droppedObject = inputfields[0].text;

		dropReciever.RecieveDrop(target, droppedObject);
	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		return droppedObject is string || droppedObject is Rect;
	}

	public override bool RecieveDrop(GameObject target, object droppedObject)
	{
		if (!TestDrop(target, droppedObject))
			return false;

		var str = droppedObject as string;
		if (str != null)
		{
			var targetParent = target.transform.parent;
			var inputField = targetParent.GetComponentInChildren<NumericInputField>();
			var index = Array.IndexOf(m_SizeFields, inputField);
			if (index > -1)
			{
				if (SetValue(str, index))
				{
					inputField.text = str;
					inputField.ForceUpdateLabel();
					return true;
				}
			}

			index = Array.IndexOf(m_CenterFields, inputField);
			if (index > -1)
			{
				if (SetValue(str, index, true))
				{
					inputField.text = str;
					inputField.ForceUpdateLabel();
					return true;
				}
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

		return false;
	}
}