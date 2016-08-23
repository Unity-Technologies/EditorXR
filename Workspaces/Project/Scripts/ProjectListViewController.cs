using UnityEngine;

public class ProjectListViewController : NestedListViewController<AssetData>
{
	protected override void Setup()
	{
		base.Setup();
		data = new []
		{
			new AssetData("test1"),
			new AssetData("test2"),
			new AssetData("test3")
		};
	}

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