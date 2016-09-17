using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InspectorBoundsItem : InspectorPropertyItem
{

	[SerializeField]
	private InputField[] m_CenterFields;
	[SerializeField]
	private InputField[] m_ExtentsFields;

	private bool m_Setup;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		if (!m_Setup)
		{
			m_Setup = true;
			for (int i = 0; i < m_CenterFields.Length; i++)
			{
				var index = i;
				m_CenterFields[i].onValueChanged.AddListener(value => SetValue(value, index, true));
				m_ExtentsFields[i].onValueChanged.AddListener(value => SetValue(value, index));
			}
		}

		for (int i = 0; i < m_CenterFields.Length; i++)
		{
			var bounds = m_SerializedProperty.boundsValue;
			m_CenterFields[i].text = bounds.center[i].ToString();
			m_ExtentsFields[i].text = bounds.extents[i].ToString();
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
}