using UnityEngine;

public class ProjectListViewController : NestedListViewController<AssetData>
{
	public AssetData[] data {set { m_Data = value;}}
	public Bounds bounds { private get; set; }

	protected override void ComputeConditions()
	{
		base.ComputeConditions();
		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward;
	}

	void OnDrawGizmos()
	{
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(bounds.center, bounds.size);
		Gizmos.DrawSphere(m_StartPosition, 0.01f);
	}

	protected override void Positioning(Transform t, int offset)
	{
		t.GetComponent<AssetListItem>().Resize(bounds.size.x);

		t.localPosition = m_StartPosition + (offset * m_ItemSize.z + scrollOffset) * Vector3.back;
		t.localRotation = Quaternion.identity;
	}
}