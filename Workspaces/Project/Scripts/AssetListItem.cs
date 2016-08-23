using System.IO;
using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;

public class AssetListItem : ListViewItem<AssetData>
{
	private const float kMargin = 0.01f;

	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private DirectHandle m_Cube;

	[SerializeField]
	private DirectHandle m_ExpandArrow;

	public override void Setup(AssetData data)
	{
		base.Setup(data);
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
}