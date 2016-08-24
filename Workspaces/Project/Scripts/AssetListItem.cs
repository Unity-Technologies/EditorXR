using System.IO;
using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;
using Object = UnityEngine.Object;

public class AssetListItem : ListViewItem<AssetData>
{
	private const float kMargin = 0.01f;

	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private DirectHandle m_Cube;

	[SerializeField]
	private DirectHandle m_ExpandArrow;

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
			m_Setup = true;
		}
		m_Text.text = Path.GetFileNameWithoutExtension(data.path);
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

		float arrowWidth = m_ExpandArrow.transform.localScale.x * 0.5f;
		float contentHeight = m_ExpandArrow.transform.localPosition.y;
		float halfWidth = width * 0.5f;
		m_ExpandArrow.transform.localPosition = new Vector3(kMargin - halfWidth, contentHeight, 0);

		m_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width - kMargin * 2) * 1 / m_Text.transform.localScale.x);
		m_Text.transform.localPosition = new Vector3(kMargin * 2 + arrowWidth - halfWidth, contentHeight, 0); //Text is next to arrow, with a margin
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

	private void OnDestroy()
	{
		U.Object.Destroy(m_CubeRenderer.sharedMaterial);
	}
}