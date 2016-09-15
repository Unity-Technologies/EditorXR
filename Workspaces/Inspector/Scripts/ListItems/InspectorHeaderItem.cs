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

	private GameObject m_GameObject;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var target = data.serializedObject.targetObject;

		m_NameField.text = target.name;
		m_Icon.texture = AssetPreview.GetMiniThumbnail(target);

		m_GameObject = target as GameObject;

		m_ActiveToggle.onValueChanged.AddListener(SetActive);
	}

	private void SetActive(bool active)
	{
		m_GameObject.SetActive(active);
	}
}