using System.Collections.Generic;
using System.Linq;
using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class InspectorListViewController : NestedListViewController<InspectorData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	private Material m_CubeMaterial;

	private readonly Dictionary<string, Vector3> m_TemplateSizes = new Dictionary<string, Vector3>();

	public override InspectorData[] data
	{
		set
		{
			base.data = value;
		}
	}

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<InspectorListItem>();
		item.GetMaterials(out m_CubeMaterial);

		foreach (var template in m_TemplateDictionary)
			m_TemplateSizes[template.Key] = GetObjectSize(template.Value.prefab);
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		m_StartPosition = bounds.extents.z * Vector3.forward;

		var parentMatrix = transform.worldToLocalMatrix;
		m_CubeMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_CubeMaterial.SetVector("_ClipExtents", bounds.extents * 100);
	}

	protected override void UpdateItems()
	{
		float totalOffset = 0;
		UpdateRecursively(m_Data, ref totalOffset);
	}

	private void UpdateRecursively(InspectorData[] data, ref float totalOffset, int depth = 0)
	{
		foreach (var item in data)
		{
			m_ItemSize = m_TemplateSizes[item.template];
			if (totalOffset + scrollOffset + m_ItemSize.z < 0)
				CleanUpBeginning(item);
			else if (totalOffset + scrollOffset > bounds.size.z)
				CleanUpEnd(item);
			else
				UpdateItemRecursive(item, totalOffset, depth);
			totalOffset += m_ItemSize.z;
			if (item.children != null)
			{
				if (item.expanded)
					UpdateRecursively(item.children, ref totalOffset, depth + 1);
				else
					RecycleChildren(item);
			}
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(Vector3.zero, bounds.size);
	}

	private void UpdateItemRecursive(InspectorData data, float offset, int depth)
	{
		if (data.item == null)
			data.item = GetItem(data);
		var item = (InspectorListItem)data.item;
		item.UpdateTransforms(bounds.size.x - kClipMargin, depth);

		UpdateItem(item.transform, offset);
	}

	private void UpdateItem(Transform t, float offset)
	{
		t.localPosition = m_StartPosition + (offset + m_ScrollOffset) * Vector3.back;
		t.localRotation = Quaternion.identity;
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