using System;
using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Utilities;

public class HierarchyListItem : ListViewItem<HierarchyData>
{
	const float kMargin = 0.01f;
	const float kIndent = 0.02f;

	const float kExpandArrowRotateSpeed = 0.4f;

	[SerializeField]
	Text m_Text;

	[SerializeField]
	BaseHandle m_Cube;

	[SerializeField]
	BaseHandle m_ExpandArrow;

	[SerializeField]
	Material m_NoClipCubeMaterial;

	[SerializeField]
	Material m_NoClipExpandArrowMaterial;

	[SerializeField]
	Color m_HoverColor;

	[SerializeField]
	Color m_SelectedColor;

	Color m_NormalColor;
	bool m_Hovering;
	Renderer m_CubeRenderer;
	Transform m_CubeTransform;

	public Material cubeMaterial { get { return m_CubeRenderer.sharedMaterial; } }
	public Action<HierarchyData> toggleExpanded { private get; set; }
	public Action<int> selectRow { private get; set; }
	
	public override void Setup(HierarchyData listData)
	{
		base.Setup(listData);
		// First time setup
		if (m_CubeRenderer == null)
		{
			// Cube material might change for hover state, so we always instance it
			m_CubeRenderer = m_Cube.GetComponent<Renderer>();
			m_NormalColor = m_CubeRenderer.sharedMaterial.color;
			U.Material.GetMaterialClone(m_CubeRenderer);

			m_ExpandArrow.dragEnded += ToggleExpanded;
			m_Cube.dragStarted += SelectFolder;
			m_Cube.dragEnded += ToggleExpanded;

			m_Cube.hoverStarted += OnHoverStarted;
			m_Cube.hoverEnded += OnHoverEnded;
		}

		m_CubeTransform = m_Cube.transform;
		m_Text.text = listData.name;

		// HACK: We need to kick the canvasRenderer to update the mesh properly
		m_Text.gameObject.SetActive(false);
		m_Text.gameObject.SetActive(true);

		m_ExpandArrow.gameObject.SetActive(listData.children != null);
		m_Hovering = false;
	}

	public void SetMaterials(Material textMaterial, Material expandArrowMaterial)
	{
		m_Text.material = textMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
	}

	public void UpdateSelf(float width, int depth, bool expanded, bool selected)
	{
		var cubeScale = m_CubeTransform.localScale;
		cubeScale.x = width;
		m_CubeTransform.localScale = cubeScale;

		var expandArrowTransform = m_ExpandArrow.transform;

		var arrowWidth = expandArrowTransform.localScale.x * 0.5f;
		var halfWidth = width * 0.5f;
		var indent = kIndent * depth;
		var doubleMargin = kMargin * 2;
		expandArrowTransform.localPosition = new Vector3(kMargin + indent - halfWidth, expandArrowTransform.localPosition.y, 0);

		// Text is next to arrow, with a margin and indent, rotated toward camera
		var textTransform = m_Text.transform;
		m_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width - doubleMargin - indent) * 1 / textTransform.localScale.x);
		textTransform.localPosition = new Vector3(doubleMargin + indent + arrowWidth - halfWidth, textTransform.localPosition.y, 0);

		textTransform.localRotation = U.Camera.LocalRotateTowardCamera(transform.parent.rotation);

		UpdateArrow(expanded);

		// Set selected/hover/normal color
		if (selected)
			m_CubeRenderer.sharedMaterial.color = m_SelectedColor;
		else if (m_Hovering)
			m_CubeRenderer.sharedMaterial.color = m_HoverColor;
		else
			m_CubeRenderer.sharedMaterial.color = m_NormalColor;
	}

	public void UpdateArrow(bool expanded, bool immediate = false)
	{
		var expandArrowTransform = m_ExpandArrow.transform;
		// Rotate arrow for expand state
		expandArrowTransform.localRotation = Quaternion.Lerp(expandArrowTransform.localRotation,
			Quaternion.AngleAxis(90f, Vector3.right) * (expanded ? Quaternion.AngleAxis(90f, Vector3.back) : Quaternion.identity),
			immediate ? 1f : kExpandArrowRotateSpeed);
	}

	void ToggleExpanded(BaseHandle handle, HandleEventData eventData)
	{
		toggleExpanded(data);
	}

	void SelectFolder(BaseHandle baseHandle, HandleEventData eventData)
	{
		selectRow(data.instanceID);
	}

	void OnHoverStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		m_Hovering = true;
	}

	void OnHoverEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		m_Hovering = false;
	}

	void OnDestroy()
	{
		if (m_CubeRenderer)
			U.Object.Destroy(m_CubeRenderer.sharedMaterial);
	}
}