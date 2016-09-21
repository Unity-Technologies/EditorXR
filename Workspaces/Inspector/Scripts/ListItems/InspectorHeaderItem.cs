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

	private GameObject m_TargetObject;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var target = data.serializedObject.targetObject;

		m_Icon.texture = AssetPreview.GetMiniThumbnail(target);

		m_TargetObject = (GameObject)target;

		m_ActiveToggle.isOn = m_TargetObject.activeSelf;
		m_NameField.text = m_TargetObject.name;
		m_StaticToggle.isOn = m_TargetObject.isStatic;

		m_ActiveToggle.onValueChanged.AddListener(SetActive);
		m_NameField.onValueChanged.AddListener(SetName);
		m_StaticToggle.onValueChanged.AddListener(SetStatic);
	}

	private void SetActive(bool active)
	{
		// TODO: Add choice dialog for whether to set in children
		if (m_TargetObject.activeSelf != active)
			m_TargetObject.SetActive(active);
	}

	private void SetName(string name)
	{
		if(!m_TargetObject.name.Equals(name))
			m_TargetObject.name = name;
	}

	private void SetStatic(bool isStatic)
	{
		// TODO: Add choice dialog for whether to set in children
		if(m_TargetObject.isStatic != isStatic)
			m_TargetObject.isStatic = isStatic;
	}
}