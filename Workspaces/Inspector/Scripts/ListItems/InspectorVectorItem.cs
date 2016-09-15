using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InspectorVectorItem : InspectorListItem
{
	[SerializeField]
	private Text m_Label;

	[SerializeField]
	private InputField[] m_InputFields;

	[SerializeField]
	private GameObject ZGroup;

	[SerializeField]
	private GameObject WGroup;

	private SerializedProperty m_SerializedProperty;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_SerializedProperty = ((PropertyData)data).property;

		m_Label.text = m_SerializedProperty.displayName;

		Vector4 vector = Vector4.zero;
		switch (m_SerializedProperty.propertyType)
		{
			case SerializedPropertyType.Vector2:
				ZGroup.SetActive(false);
				WGroup.SetActive(false);
				vector = m_SerializedProperty.vector2Value;
				break;
			case SerializedPropertyType.Quaternion:
				vector = m_SerializedProperty.quaternionValue.eulerAngles;
				WGroup.SetActive(false);
				break;
			case SerializedPropertyType.Vector3:
				vector = m_SerializedProperty.vector3Value;
				WGroup.SetActive(false);
				break;
			case SerializedPropertyType.Vector4:
				vector = m_SerializedProperty.vector4Value;
				break;
		}

		for (int i = 0; i < m_InputFields.Length; i++)
		{
			var index = i;
			m_InputFields[i].onValueChanged.RemoveAllListeners();
			m_InputFields[i].text = vector[i].ToString();
			m_InputFields[i].onValueChanged.AddListener(value => SetValue(value, index));
		}
	}

	private void SetValue(string input, int index)
	{
		float value;
		if (float.TryParse(input, out value))
		{
			switch (m_SerializedProperty.propertyType)
			{
				case SerializedPropertyType.Vector2:
					var vector2 = m_SerializedProperty.vector2Value;
					vector2[index] = value;
					m_SerializedProperty.vector2Value = vector2;
					break;
				case SerializedPropertyType.Vector3:
					var vector3 = m_SerializedProperty.vector3Value;
					vector3[index] = value;
					m_SerializedProperty.vector3Value = vector3;
					break;
				case SerializedPropertyType.Vector4:
					var vector4 = m_SerializedProperty.vector4Value;
					vector4[index] = value;
					m_SerializedProperty.vector4Value = vector4;
					break;
				case SerializedPropertyType.Quaternion:
					var euler = m_SerializedProperty.quaternionValue.eulerAngles;
					euler[index] = value;
					m_SerializedProperty.quaternionValue = Quaternion.Euler(euler);
					break;
			}

			data.serializedObject.ApplyModifiedProperties();
		}
	}
}