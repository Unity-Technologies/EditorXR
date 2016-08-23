using UnityEngine;

public class ProjectListViewController : NestedListViewController<AssetData>
{
	public AssetData[] data {set { m_Data = value;}}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();
		m_StartPosition = Vector3.zero;
	}

	protected override void UpdateRecursively(AssetData[] data, ref int count) {
		foreach (var item in data)
		{
			if (item.children == null) //Skip files, only list folders
				continue;
			if (count + m_DataOffset < 0) {
				ExtremeLeft(item);
			} else if (count + m_DataOffset > m_NumItems) {
				ExtremeRight(item);
			} else {
				ListMiddle(item, count + m_DataOffset);
			}
			count++;
			if (item.children != null) {
				if (item.expanded) {
					UpdateRecursively(item.children, ref count);
				} else {
					RecycleChildren(item);
				}
			}
		}
	}

	protected override void Positioning(Transform t, int offset) {
		t.localPosition = m_StartPosition + (offset * m_ItemSize.z + scrollOffset) * Vector3.forward;
		t.localRotation = Quaternion.identity;
	}
}