using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class InspectorListViewController : NestedListViewController<InspectorData>
{
	private Material m_TextMaterial;
	private Material m_ExpandArrowMaterial;

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<InspectorListItem>();
		item.GetMaterials(out m_TextMaterial, out m_ExpandArrowMaterial);
	}

	public InspectorData[] listData
	{
		set
		{
			if (m_Data != null) // Clear out visuals for old data
			{
				foreach (var data in m_Data)
				{
					CleanUpBeginning(data);
				}
			}
			m_ScrollOffset = 0;
			m_Data = value;
		}
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		var parentMatrix = transform.worldToLocalMatrix;
		m_TextMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_TextMaterial.SetVector("_ClipExtents", bounds.extents);
		m_ExpandArrowMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_ExpandArrowMaterial.SetVector("_ClipExtents", bounds.extents);
	}

	protected override ListViewItem<InspectorData> GetItem(InspectorData listData)
	{
		var item = (InspectorListItem)base.GetItem(listData);
		item.SwapMaterials(m_TextMaterial, m_ExpandArrowMaterial);
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
	}
}