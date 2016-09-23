using UnityEditor;
using UnityEngine;

public class InspectorVectorItem : InspectorPropertyItem
{
	[SerializeField]
	private NumericInputField[] m_InputFields;

	[SerializeField]
	private GameObject ZGroup;

	[SerializeField]
	private GameObject WGroup;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		Vector4 vector = Vector4.zero;
		int count = 4;
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.Vector2:
				ZGroup.SetActive(false);
				WGroup.SetActive(false);
				vector = m_SerializedProperty.vector2Value;
				count = 2;
				break;
			case SerializedPropertyType.Quaternion:
				vector = m_SerializedProperty.quaternionValue.eulerAngles;
				ZGroup.SetActive(true);
				WGroup.SetActive(false);
				count = 3;
				break;
			case SerializedPropertyType.Vector3:
				vector = m_SerializedProperty.vector3Value;
				ZGroup.SetActive(true);
				WGroup.SetActive(false);
				count = 3;
				break;
			case SerializedPropertyType.Vector4:
				vector = m_SerializedProperty.vector4Value;
				ZGroup.SetActive(true);
				WGroup.SetActive(true);
				break;
		}

		for (int i = 0; i < count; i++)
			m_InputFields[i].text = vector[i].ToString();
	}

	protected override void FirstTimeSetup()
	{
		base.FirstTimeSetup();

		//TODO: expose onValueChanged in Inspector
		for (int i = 0; i < m_InputFields.Length; i++)
		{
			var index = i;
			m_InputFields[i].onValueChanged.AddListener(value => SetValue(value, index));
		}
	}

	public void SetValue(string input, int index)
	{
		float value;
		if (!float.TryParse(input, out value)) return;
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.Vector2:
				var vector2 = m_SerializedProperty.vector2Value;
				if (vector2[index] != value)
				{
					vector2[index] = value;
					m_SerializedProperty.vector2Value = vector2;
				}
				break;
			case SerializedPropertyType.Vector3:
				var vector3 = m_SerializedProperty.vector3Value;
				if (vector3[index] != value)
				{
					vector3[index] = value;
					m_SerializedProperty.vector3Value = vector3;
				}
				break;
			case SerializedPropertyType.Vector4:
				var vector4 = m_SerializedProperty.vector4Value;
				if (vector4[index] != value)
				{
					vector4[index] = value;
					m_SerializedProperty.vector4Value = vector4;
				}
				break;
			case SerializedPropertyType.Quaternion:
				var euler = m_SerializedProperty.quaternionValue.eulerAngles;
				if (euler[index] != value)
				{
					euler[index] = value;
					m_SerializedProperty.quaternionValue = Quaternion.Euler(euler);
				}
				break;
		}

		data.serializedObject.ApplyModifiedProperties();
	}
}