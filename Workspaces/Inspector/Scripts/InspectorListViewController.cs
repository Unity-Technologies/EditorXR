using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class InspectorListViewController : NestedListViewController<InspectorData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	private Material m_CubeMaterial;

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<InspectorListItem>();
		item.GetMaterials(out m_CubeMaterial);
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		var parentMatrix = transform.worldToLocalMatrix;
		m_CubeMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_CubeMaterial.SetVector("_ClipExtents", bounds.extents);
	}

	protected override void UpdateItemRecursive(InspectorData data, int offset, int depth)
	{
		if (data.item == null)
			data.item = GetItem(data);
		var item = (InspectorListItem)data.item;
		item.UpdateTransforms(bounds.size.x - kClipMargin, depth);

		UpdateItem(item.transform, offset);
	}

	protected override ListViewItem<InspectorData> GetItem(InspectorData listData)
	{
		var item = (InspectorListItem)base.GetItem(listData);
		item.SwapMaterials(m_CubeMaterial);
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_CubeMaterial);
	}
}