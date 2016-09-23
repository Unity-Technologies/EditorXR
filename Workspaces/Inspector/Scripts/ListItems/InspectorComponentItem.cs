using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InspectorComponentItem : InspectorListItem
{
	[SerializeField]
	private Button m_ExpandArrow;

	[SerializeField]
	private RawImage m_Icon;

	[SerializeField]
	private Toggle m_EnabledToggle;

	[SerializeField]
	private Text m_NameText;

	[SerializeField]
	private Button m_GearMenu;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var target = data.serializedObject.targetObject;
		var type = target.GetType();
		m_NameText.text = type.Name;
		m_Icon.texture = AssetPreview.GetMiniTypeThumbnail(type);

		m_EnabledToggle.gameObject.SetActive(EditorUtility.GetObjectEnabled(target) != -1);
		m_EnabledToggle.isOn = EditorUtility.GetObjectEnabled(target) == 1;

		m_ExpandArrow.gameObject.SetActive(data.children != null);
	}

	public void GetMaterials(out Material textMaterial, out Material expandArrowMaterial, out Material gearMaterial)
	{
		textMaterial = Instantiate(m_NameText.material);
		expandArrowMaterial = Instantiate(m_ExpandArrow.GetComponent<Renderer>().sharedMaterial);
		gearMaterial = Instantiate(m_GearMenu.GetComponent<Renderer>().sharedMaterial);
	}

	public void SwapMaterials(Material textMaterial, Material expandArrowMaterial, Material gearMaterial)
	{
		m_NameText.material = textMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
		m_GearMenu.GetComponent<Renderer>().sharedMaterial = gearMaterial;
	}

	public void ToggleExpanded()
	{
		data.expanded = !data.expanded;
	}

	public void SetEnabled(bool value)
	{
		EditorUtility.SetObjectEnabled(data.serializedObject.targetObject, value);
	}
}