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

	private bool m_Setup;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		if (!m_Setup)
		{
			m_Setup = true;
			m_ExpandArrow.dragEnded += ToggleExpanded;
		}

		var type = data.serializedObject.targetObject.GetType();
		m_NameText.text = type.Name;
		m_Icon.texture = AssetPreview.GetMiniTypeThumbnail(type);

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

	private void ToggleExpanded(BaseHandle handle, HandleEventData eventData)
	{
		data.expanded = !data.expanded;
	}
}