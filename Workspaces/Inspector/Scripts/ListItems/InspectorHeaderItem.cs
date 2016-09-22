using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InspectorHeaderItem : InspectorListItem
{
	[SerializeField]
	private RawImage m_Icon;

	[SerializeField]
	private Toggle m_ActiveToggle;

	[SerializeField]
	private InputField m_NameField;

	[SerializeField]
	private Toggle m_StaticToggle;

	// TODO: Add dropdown for different static types
	
	private GameObject m_TargetGameObject;

	private bool m_Setup;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		if (!m_Setup)
		{
			m_Setup = true;
			m_ActiveToggle.onValueChanged.AddListener(SetActive);
			m_NameField.onValueChanged.AddListener(SetName);
			m_StaticToggle.onValueChanged.AddListener(SetStatic);
		}

		var target = data.serializedObject.targetObject;

		m_Icon.texture = AssetPreview.GetMiniThumbnail(target);
		m_TargetGameObject = target as GameObject;

		if (m_TargetGameObject)
		{
			m_ActiveToggle.isOn = m_TargetGameObject.activeSelf;
			m_StaticToggle.isOn = m_TargetGameObject.isStatic;
		}

		m_NameField.text = target.name;
	}

	private void SetActive(bool active)
	{
		// TODO: Add choice dialog for whether to set in children
		if (m_TargetGameObject.activeSelf != active)
			m_TargetGameObject.SetActive(active);
	}

	private void SetName(string name)
	{
		var target = data.serializedObject.targetObject;
		if (!target.name.Equals(name))
			target.name = name;
	}

	private void SetStatic(bool isStatic)
	{
		// TODO: Add choice dialog for whether to set in children
		if(m_TargetGameObject.isStatic != isStatic)
			m_TargetGameObject.isStatic = isStatic;
	}
}