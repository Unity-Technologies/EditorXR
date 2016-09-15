using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;

public class InspectorComponentItem : InspectorListItem
{
	[SerializeField]
	private BaseHandle m_ExpandArrow;

	[SerializeField]
	private RawImage m_Icon;

	[SerializeField]
	private Toggle m_EnabledToggle;

	[SerializeField]
	private Text m_NameText;

	[SerializeField]
	private BaseHandle m_GearMenu;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var type = data.serializedObject.targetObject.GetType();
		m_NameText.text = type.Name;
		m_Icon.texture = AssetPreview.GetMiniTypeThumbnail(type);
	}
}