using UnityEngine;
using UnityEngine.VR.Modules;

public class InspectorBoundsItem : InspectorPropertyItem
{
	[SerializeField]
	private NumericInputField[] m_CenterFields;
	[SerializeField]
	private NumericInputField[] m_ExtentsFields;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		for (int i = 0; i < m_CenterFields.Length; i++)
		{
			var bounds = m_SerializedProperty.boundsValue;
			m_CenterFields[i].text = bounds.center[i].ToString();
			m_ExtentsFields[i].text = bounds.extents[i].ToString();
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
			m_ExtentsFields[i].onValueChanged.AddListener(value => SetValue(value, index));
		}
	}

	private void SetValue(string input, int index, bool center = false)
	{
		float value;
		if (!float.TryParse(input, out value)) return;

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
	}

	protected override void DropItem(Transform fieldBlock, IDropReciever dropReciever, GameObject target)
	{

	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		return false;
	}

	public override bool RecieveDrop(GameObject target, object droppedObject)
	{
		return false;
	}
}