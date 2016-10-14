using ListView;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public class InspectorListViewController : NestedListViewController<InspectorData>, IPreview, IHighlight
{
	const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	[SerializeField]
	Material m_RowCubeMaterial;

	[SerializeField]
	Material m_BackingCubeMaterial;

	[SerializeField]
	Material m_TextMaterial;

	[SerializeField]
	Material m_UIMaterial;

	[SerializeField]
	Material m_NoClipBackingCube;

	readonly Dictionary<string, Vector3> m_TemplateSizes = new Dictionary<string, Vector3>();

	public Action<GameObject, bool> setHighlight { private get; set; }

	public PreviewDelegate preview { private get; set; }
	public Func<Transform, Transform> getPreviewOriginForRayOrigin { private get; set; }

	public Func<bool> getIsLocked { private get; set; }
	public Action<bool> setIsLocked { private get; set; }

	public event Action<InspectorData[], PropertyData> arraySizeChanged = delegate {};

	protected override void Setup()
	{
		base.Setup();

		m_RowCubeMaterial = Instantiate(m_RowCubeMaterial);
		m_BackingCubeMaterial = Instantiate(m_BackingCubeMaterial);
		m_TextMaterial = Instantiate(m_TextMaterial);
		m_UIMaterial = Instantiate(m_UIMaterial);

		foreach (var template in m_TemplateDictionary)
			m_TemplateSizes[template.Key] = GetObjectSize(template.Value.prefab);

		data = new InspectorData[0];
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		m_StartPosition = bounds.extents.z * Vector3.back;

		var parentMatrix = transform.worldToLocalMatrix;
		SetMaterialClip(m_RowCubeMaterial, parentMatrix);
		SetMaterialClip(m_BackingCubeMaterial, parentMatrix);
		SetMaterialClip(m_TextMaterial, parentMatrix);
		SetMaterialClip(m_UIMaterial, parentMatrix);
	}

	protected override void UpdateItems()
	{
		var totalOffset = 0f;
		UpdateRecursively(m_Data, ref totalOffset);
		// Snap back if list scrolled too far
		if (totalOffset > 0 && -scrollOffset >= totalOffset)
			m_ScrollReturn = -totalOffset + m_ItemSize.z; // m_ItemSize will be equal to the size of the last visible item
	}

	void UpdateRecursively(InspectorData[] data, ref float totalOffset, int depth = 0)
	{
		foreach (var item in data)
		{
			m_ItemSize = m_TemplateSizes[item.template];
			if (totalOffset + scrollOffset + m_ItemSize.z < 0)
				RecycleBeginning(item);
			else if (totalOffset + scrollOffset > bounds.size.z)
				RecycleEnd(item);
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

	void UpdateItemRecursive(InspectorData data, float offset, int depth)
	{
		if (data.item == null)
			data.item = GetItem(data);
		var item = (InspectorListItem)data.item;
		item.UpdateSelf(bounds.size.x - kClipMargin, depth);
		item.UpdateClipTexts(transform.worldToLocalMatrix, bounds.extents);

		UpdateItem(item.transform, offset);
	}

	void UpdateItem(Transform t, float offset)
	{
		t.localPosition = m_StartPosition + (offset + m_ScrollOffset) * Vector3.forward;
		t.localRotation = Quaternion.identity;
	}

	protected override ListViewItem<InspectorData> GetItem(InspectorData listData)
	{
		var item = (InspectorListItem)base.GetItem(listData);
		if (!item.setup)
		{
			item.SetMaterials(m_RowCubeMaterial, m_BackingCubeMaterial, m_UIMaterial, m_TextMaterial, m_NoClipBackingCube);

			item.setHighlight = setHighlight;
			item.preview = preview;
			item.getPreviewOriginForRayOrigin = getPreviewOriginForRayOrigin;

			var numberItem = item as InspectorNumberItem;
			if (numberItem)
				numberItem.arraySizeChanged += OnArraySizeChanged;

			item.setup = true;
		}

		var headerItem = item as InspectorHeaderItem;
		if (headerItem)
		{
			headerItem.lockToggle.isOn = getIsLocked();
			headerItem.setLocked = setIsLocked;
		}

		return item;
	}

	void OnArraySizeChanged(PropertyData element)
	{
		arraySizeChanged(m_Data, element);
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_RowCubeMaterial);
		U.Object.Destroy(m_BackingCubeMaterial);
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_UIMaterial);
	}
}