using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Tools;

public class InspectorListItem : DraggableListItem<InspectorData>, IHighlight, IDroppable, IDropReciever
{
	private const float kIndent = 0.02f;

	protected CuboidLayout m_CuboidLayout;

	[SerializeField]
	private BaseHandle m_Cube;

	[SerializeField]
	private RectTransform m_UIContainer;

	private ClipText[] m_ClipTexts;

	private Material m_NoClipBackingCube;

	private bool m_Setup;

	public bool setup { get; set; }

	public Action<GameObject, bool> setHighlight { set; private get; }

	public Func<Transform, IDropReciever> getCurrentDropReciever { protected get; set; }

	public Action<Transform, IDropReciever> setCurrentDropReciever { private get; set; }

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		if (!m_Setup)
		{
			m_Setup = true;
			FirstTimeSetup();
		}
	}

	protected virtual void FirstTimeSetup()
	{
		m_ClipTexts = GetComponentsInChildren<ClipText>(true);
		m_CuboidLayout = GetComponentInChildren<CuboidLayout>(true);
		if(m_CuboidLayout)
			m_CuboidLayout.UpdateCubes();

		var handles = GetComponentsInChildren<BaseHandle>(true);
		foreach (var handle in handles)
		{
			// Ignore m_Cube for now (will be used for Reset action)
			if(handle.Equals(m_Cube))
				continue;

			handle.dragStarted += OnDragStarted;
			handle.dragging += OnDragging;
			handle.dragEnded += OnDragEnded;

			handle.hoverStarted += OnHoverStarted;
			handle.hoverEnded += OnHoverEnded;
		}
	}

	public void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material UIMaterial, Material textMaterial, Material noClipBackingCube)
	{
		m_NoClipBackingCube = noClipBackingCube;

		m_Cube.GetComponent<Renderer>().sharedMaterial = rowMaterial;

		var cuboidLayouts = GetComponentsInChildren<CuboidLayout>(true);
		foreach (var cuboidLayout in cuboidLayouts)
			cuboidLayout.SetMaterials(backingCubeMaterial);

		var graphics = GetComponentsInChildren<Graphic>(true);
		foreach (var graphic in graphics)
			graphic.material = UIMaterial;

		// Texts need a specific shader
		var texts = GetComponentsInChildren<Text>(true);
		foreach (var text in texts)
			text.material = textMaterial;

		// Don't clip masks
		var masks = GetComponentsInChildren<Mask>(true);
		foreach (var mask in masks)
			mask.graphic.material = null;
	}

	public virtual void UpdateSelf(float width, int depth)
	{
		var cubeScale = m_Cube.transform.localScale;
		cubeScale.x = width;
		m_Cube.transform.localScale = cubeScale;

		var indent = kIndent * depth;
		m_UIContainer.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, indent, width - indent);
	}

	public void UpdateClipTexts(Matrix4x4 parentMatrix, Vector3 clipExtents)
	{
		foreach (var clipText in m_ClipTexts)
		{
			clipText.clipExtents = clipExtents;
			clipText.parentMatrix = parentMatrix;
			clipText.UpdateMaterialClip();
		}
	}

	protected virtual void OnHoverStarted(BaseHandle handle, HandleEventData eventData)
	{
		setHighlight(handle.gameObject, true);
		setCurrentDropReciever(eventData.rayOrigin, this);
	}

	protected virtual void OnHoverEnded(BaseHandle handle, HandleEventData eventData)
	{
		setHighlight(handle.gameObject, false);
		setCurrentDropReciever(eventData.rayOrigin, null);
	}

	protected override void OnDragStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		base.OnDragStarted(baseHandle, eventData);

		var clone = Instantiate(baseHandle.transform.parent.gameObject, baseHandle.transform.parent.parent) as GameObject;
		// Re-center pivot
		clone.GetComponent<RectTransform>().pivot = Vector2.one * 0.5f;

		//Re-center backing cube
		foreach (Transform child in clone.transform)
		{
			if (child.GetComponent<BaseHandle>())
			{
				var localPos = child.localPosition;
				localPos.x = 0;
				localPos.y = 0;
				child.localPosition = localPos;
			}
		}

		m_DragObject = clone.transform;

		var graphics = clone.GetComponentsInChildren<Graphic>(true);
		foreach (var graphic in graphics)
			graphic.material = null;

		var renderers = clone.GetComponentsInChildren<Renderer>(true);
		foreach (var renderer in renderers)
			renderer.sharedMaterial = m_NoClipBackingCube;
	}

	public virtual bool OnDrop(object droppedObject)
	{
		return false;
	}
}