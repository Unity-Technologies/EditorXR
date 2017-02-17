using UnityEngine;
using UnityEngine.Experimental.EditorVR.Data;
using UnityEngine.UI;

public class InspectorBoolItem : InspectorPropertyItem
{
	[SerializeField]
	Toggle m_Toggle;

#if UNITY_EDITOR
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

	public override void OnObjectModified()
	{
		base.OnObjectModified();
		m_Toggle.isOn = m_SerializedProperty.boolValue;
	}

	public void SetValue(bool value)
	{
		if (m_SerializedProperty.boolValue != value)
		{
			m_SerializedProperty.boolValue = value;

			FinalizeModifications();
		}
	}
#endif
}