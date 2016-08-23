using ListView;

public class AssetData : ListViewItemNestedData<AssetData>
{
	private const string kTemplateName = "AssetItem";
	public string path {get { return m_Path;} }
	private string m_Path;

	public AssetData(string path)
	{
		template = kTemplateName;
		m_Path = path;
	}
}