using System.IO;
using ListView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;

public class AssetListItem : ListViewItem<AssetData>
{
	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private DirectHandle m_Cube;

	[SerializeField]
	private DirectHandle m_ExpandArrow;

	public override void Setup(AssetData data)
	{
		base.Setup(data);
		m_Text.text = Path.GetFileName(data.path);
	}
}