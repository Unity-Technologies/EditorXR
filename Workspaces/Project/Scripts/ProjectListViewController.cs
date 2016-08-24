using UnityEngine;

public class ProjectListViewController : NestedListViewController<AssetData>
{
	public AssetData[] data {set { m_Data = value;}}
	public Bounds bounds { private get; set; }

	protected override void ComputeConditions()
	{
		if (templates.Length > 0) {
			//Use first template to get item size
			m_ItemSize = GetObjectSize(templates[0]);
		}
		//Resize range to nearest multiple of item width
		m_NumItems = Mathf.RoundToInt(range / m_ItemSize.z); //Number of cards that will fit
		range = m_NumItems * m_ItemSize.z;

		//Get initial conditions. This procedure is done every frame in case the collider bounds change at runtime
		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward;

		m_DataOffset = (int)(scrollOffset / itemSize.z);
		if (scrollOffset < 0)
			m_DataOffset--;
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