using System.IO;
using ListView;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;
using Object = UnityEngine.Object;

public class AssetListItem : ListViewItem<AssetData>
{
	private const float kMargin = 0.01f;
	private const float kIndent = 0.02f;

	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private DirectHandle m_Cube;

	[SerializeField]
	private DirectHandle m_ExpandArrow;

	[SerializeField]
	private Material m_NoClipCubeMaterial;

	[SerializeField]
	private Material m_NoClipExpandArrowMaterial;

	private Renderer m_CubeRenderer;
	private bool m_Setup;
	
	public override void Setup(AssetData data)
	{
		base.Setup(data);
		//First time setup
		if (!m_Setup) {
			//Cube material might change, so we always instance it
			m_CubeRenderer = m_Cube.GetComponent<Renderer>();
			U.Material.GetMaterialClone(m_CubeRenderer);

			m_ExpandArrow.onHandleEndDrag += ToggleExpanded;
			m_Cube.onHandleBeginDrag += Grab;

			m_Setup = true;
		}

		m_Text.text = Path.GetFileName(data.path);
		if (data.children != null)
		{
			m_ExpandArrow.gameObject.SetActive(true);
		}
		else
		{
			m_ExpandArrow.gameObject.SetActive(false);
		}
	}

	public void SwapMaterials(Material textMaterial, Material expandArrowMaterial)
	{
		m_Text.material = textMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
	}

	public void Resize(float width)
	{
		Vector3 cubeScale = m_Cube.transform.localScale;
		cubeScale.x = width;
		m_Cube.transform.localScale = cubeScale;

		var arrowWidth = m_ExpandArrow.transform.localScale.x * 0.5f;
		var contentHeight = m_ExpandArrow.transform.localPosition.y;
		var halfWidth = width * 0.5f;
		var indent = kIndent * data.treeDepth;
		var doubleMargin = kMargin * 2;
		m_ExpandArrow.transform.localPosition = new Vector3(kMargin + indent - halfWidth, contentHeight, 0);

		m_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width - doubleMargin - indent) * 1 / m_Text.transform.localScale.x);
		m_Text.transform.localPosition = new Vector3(doubleMargin + indent + arrowWidth - halfWidth, contentHeight, 0); //Text is next to arrow, with a margin and indent
	}

	public void GetMaterials(out Material textMaterial, out Material expandArrowMaterial)
	{
		textMaterial = Object.Instantiate(m_Text.material);
		expandArrowMaterial = Object.Instantiate(m_ExpandArrow.GetComponent<Renderer>().sharedMaterial);
	}

	public void Clip(Bounds bounds, Matrix4x4 parentMatrix)
	{
		m_CubeRenderer.sharedMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_CubeRenderer.sharedMaterial.SetVector("_ClipExtents",  bounds.extents);
	}

	private void ToggleExpanded(BaseHandle baseHandle, HandleDragEventData handleDragEventData)
	{
		data.expanded = !data.expanded;
	}

	private void Grab(BaseHandle baseHandle, HandleDragEventData eventData)
	{
		var clone = (GameObject)Instantiate(gameObject, transform.position, transform.rotation, eventData.rayOrigin);
		var cloneItem = clone.GetComponent<AssetListItem>();
		cloneItem.m_Cube.GetComponent<Renderer>().sharedMaterial = m_NoClipCubeMaterial;
		cloneItem.m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = m_NoClipExpandArrowMaterial;
		cloneItem.m_Text.material = null;
	}

	private void OnDestroy()
	{
		if(m_CubeRenderer)
			U.Object.Destroy(m_CubeRenderer.sharedMaterial);
	}
}