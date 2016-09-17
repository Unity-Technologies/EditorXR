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

	private GameObject m_GameObject;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var target = data.serializedObject.targetObject;

		m_Icon.texture = AssetPreview.GetMiniThumbnail(target);

		m_GameObject = (GameObject)target;

		m_ActiveToggle.isOn = m_GameObject.activeSelf;
		m_NameField.text = m_GameObject.name;
		m_StaticToggle.isOn = m_GameObject.isStatic;

		m_ActiveToggle.onValueChanged.AddListener(SetActive);
		m_NameField.onValueChanged.AddListener(SetName);
		m_StaticToggle.onValueChanged.AddListener(SetStatic);
	}

	private void SetActive(bool active)
	{
		// TODO: Add choice dialog for whether to set in children
		if (m_GameObject.activeSelf != active)
			m_GameObject.SetActive(active);
	}

	private void SetName(string name)
	{
		if(!m_GameObject.name.Equals(name))
			m_GameObject.name = name;
	}

	private void SetStatic(bool isStatic)
	{
		// TODO: Add choice dialog for whether to set in children
		if(m_GameObject.isStatic != isStatic)
			m_GameObject.isStatic = isStatic;
	}
}