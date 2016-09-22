using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;

public class InspectorListItem : ListViewItem<InspectorData>
{
	private const float kIndent = 0.01f;

	[SerializeField]
	private BaseHandle m_Cube;

	[SerializeField]
	private RectTransform m_UIContainer;

	public bool hasMaterials { get; private set; }

	public void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material uiMaterial, Material textMaterial)
	{
		m_Cube.GetComponent<Renderer>().sharedMaterial = rowMaterial;

		var cuboidLayouts = GetComponentsInChildren<CuboidLayout>(true);
		foreach (var cuboidLayout in cuboidLayouts)
			cuboidLayout.SwapMaterials(backingCubeMaterial);

		var graphics = GetComponentsInChildren<Graphic>(true);
		foreach (var graphic in graphics)
			graphic.material = uiMaterial;

		// Texts need a specific shader
		var texts = GetComponentsInChildren<Text>(true);
		foreach (var text in texts)
			text.material = textMaterial;

		hasMaterials = true;
	}

	public void UpdateTransforms(float width, int depth)
	{
		Vector3 cubeScale = m_Cube.transform.localScale;
		cubeScale.x = width;
		m_Cube.transform.localScale = cubeScale;
	}
}