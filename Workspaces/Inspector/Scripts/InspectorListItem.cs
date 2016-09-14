using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class InspectorListItem : ListViewItem<InspectorData>
{
	private const float kMargin = 0.01f;
	private const float kIndent = 0.02f;

	private const float kExpandArrowRotateSpeed = 0.4f;

	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private BaseHandle m_ExpandArrow;

	[SerializeField]
	private BaseHandle m_Cube;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);
		m_Text.text = data.name;
	}

	public void SwapMaterials(Material textMaterial, Material expandArrowMaterial, Material cubeMaterial)
	{
		m_Text.material = textMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
		m_Cube.GetComponent<Renderer>().sharedMaterial = cubeMaterial;
	}

	public void GetMaterials(out Material textMaterial, out Material expandArrowMaterial, out Material cubeMaterial)
	{
		textMaterial = Instantiate(m_Text.material);
		expandArrowMaterial = Instantiate(m_ExpandArrow.GetComponent<Renderer>().sharedMaterial);
		cubeMaterial = Instantiate(m_Cube.GetComponent<Renderer>().sharedMaterial);
	}

	public void UpdateTransforms(float width, int depth)
	{
		Vector3 cubeScale = m_Cube.transform.localScale;
		cubeScale.x = width;
		m_Cube.transform.localScale = cubeScale;

		var arrowWidth = m_ExpandArrow.transform.localScale.x * 0.5f;
		var halfWidth = width * 0.5f;
		var indent = kIndent * depth;
		var doubleMargin = kMargin * 2;
		m_ExpandArrow.transform.localPosition = new Vector3(kMargin + indent - halfWidth, m_ExpandArrow.transform.localPosition.y, 0);

		// Text is next to arrow, with a margin and indent, rotated toward camera
		m_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width - doubleMargin - indent) * 1 / m_Text.transform.localScale.x);
		m_Text.transform.localPosition = new Vector3(doubleMargin + indent + arrowWidth - halfWidth, m_Text.transform.localPosition.y, 0);

		m_Text.transform.localRotation = U.Camera.LocalRotateTowardCamera(transform.parent.rotation);

		// Rotate arrow for expand state
		m_ExpandArrow.transform.localRotation = Quaternion.Lerp(m_ExpandArrow.transform.localRotation,
												Quaternion.AngleAxis(90f, Vector3.right) * (data.expanded ? Quaternion.AngleAxis(90f, Vector3.back) : Quaternion.identity),
												kExpandArrowRotateSpeed);
	}
}