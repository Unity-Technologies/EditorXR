using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;

public class InspectorListItem : ListViewItem<InspectorData>
{
	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private BaseHandle m_ExpandArrow;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);
		m_Text.text = data.name;
	}

	public void SwapMaterials(Material textMaterial, Material expandArrowMaterial)
	{
		m_Text.material = textMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
	}

	public void GetMaterials(out Material textMaterial, out Material expandArrowMaterial)
	{
		textMaterial = Instantiate(m_Text.material);
		expandArrowMaterial = Instantiate(m_ExpandArrow.GetComponent<Renderer>().sharedMaterial);
	}
}