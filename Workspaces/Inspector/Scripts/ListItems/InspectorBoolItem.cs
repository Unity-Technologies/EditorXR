using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;

public class InspectorBoolItem : InspectorPropertyItem
{
	[SerializeField]
	private Toggle m_Toggle;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_Toggle.isOn = m_SerializedProperty.boolValue;
	}

	protected override void FirstTimeSetup()
	{
		base.FirstTimeSetup();

		m_Toggle.onValueChanged.AddListener(SetValue);
	}

	public void SetValue(bool value)
	{
		if (m_SerializedProperty.boolValue != value)
		{
			m_SerializedProperty.boolValue = value;
			data.serializedObject.ApplyModifiedProperties();
		}
	}

	protected override void OnDragStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		// Bools cannot be dragged and droppped
	}

	protected override object GetDropObject(Transform fieldBlock)
	{
		// Bools cannot be dragged and droppped
		return null;
	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		// Bools cannot be dragged and droppped
		return false;
	}

	public override bool RecieveDrop(GameObject target, object droppedObject)
	{
		// Bools cannot be dragged and droppped
		return false;
	}
}