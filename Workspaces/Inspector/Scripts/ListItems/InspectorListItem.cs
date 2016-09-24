using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;

public class InspectorListItem : ListViewItem<InspectorData>
{
	private const float kIndent = 0.02f;

	[SerializeField]
	private BaseHandle m_Cube;

	[SerializeField]
	private RectTransform m_UIContainer;

	private ClipText[] m_ClipTexts;

	private bool m_Setup;

	public bool hasMaterials { get; private set; }

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		if (!m_Setup)
		{
			m_Setup = true;
			FirstTimeSetup();
		}

		// Touch UI width to generate cubes
		m_UIContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
	}

	protected virtual void FirstTimeSetup()
	{
		m_ClipTexts = GetComponentsInChildren<ClipText>(true);
	}

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

		// Don't clip masks
		var masks = GetComponentsInChildren<Mask>(true);
		foreach (var mask in masks)
			mask.graphic.material = null;

		hasMaterials = true;
	}

	public virtual void UpdateSelf(float width, int depth)
	{
		Vector3 cubeScale = m_Cube.transform.localScale;
		cubeScale.x = width;
		m_Cube.transform.localScale = cubeScale;

		var indent = kIndent * depth;
		//m_UIContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
		m_UIContainer.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, indent, width - indent);
	}

	public void UpdateClipTexts(Matrix4x4 parentMatrix, Vector3 clipExtents)
	{
		foreach (var clipText in m_ClipTexts)
		{
			clipText.clipExtents = clipExtents;
			clipText.parentMatrix = parentMatrix;
			clipText.UpdateMaterialClip();
		}
	}
}