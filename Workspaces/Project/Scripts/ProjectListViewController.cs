using UnityEngine;

public class ProjectListViewController : NestedListViewController<AssetData>
{
	public AssetData[] data {set { m_Data = value;}}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();
		m_StartPosition = Vector3.zero;
	}

	protected override void Positioning(Transform t, int offset) {
		t.localPosition = m_StartPosition + (offset * m_ItemSize.z + scrollOffset) * Vector3.forward;
		t.localRotation = Quaternion.identity;
	}
}