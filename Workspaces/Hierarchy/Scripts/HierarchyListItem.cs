using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.UI;

public class HierarchyListItem : DraggableListItem<HierarchyData>
{
	const float kMargin = 0.01f;
	const float kIndent = 0.02f;

	const float kExpandArrowRotateSpeed = 0.4f;

	protected override bool singleClickDrag { get { return false; } }

	[SerializeField]
	Text m_Text;

	[SerializeField]
	BaseHandle m_Cube;

	[SerializeField]
	BaseHandle m_ExpandArrow;

	[SerializeField]
	BaseHandle m_DropZone;

	[SerializeField]
	Material m_NoClipCubeMaterial;

	[SerializeField]
	Material m_NoClipExpandArrowMaterial;

	[SerializeField]
	Color m_HoverColor;

	[SerializeField]
	Color m_SelectedColor;

	Material m_NoClipBackingCube;

	Color m_NormalColor;
	bool m_Hovering;
	Transform m_CubeTransform;
	Transform m_DropZoneTransform;

	float m_DropZoneHighlightAlpha;

	public Material cubeMaterial { get; private set; }
	public Material dropZoneMaterial { get; private set; }

	public Action<int> toggleExpanded { private get; set; }
	public Action<int, bool> setExpanded { private get; set; }
	public Action<int> selectRow { private get; set; }

	public Func<int, bool> isExpanded { private get; set; }
	
	public override void Setup(HierarchyData listData)
	{
		base.Setup(listData);
		// First time setup
		if (cubeMaterial == null)
		{
			// Cube material might change for hover state, so we always instance it
			var cubeRenderer = m_Cube.GetComponent<Renderer>();
			cubeMaterial = U.Material.GetMaterialClone(cubeRenderer);
			m_NormalColor = cubeMaterial.color;

			m_ExpandArrow.dragEnded += ToggleExpanded;
			m_Cube.dragStarted += OnDragStarted;
			m_Cube.dragging += OnDragging;
			m_Cube.dragEnded += OnDragEnded;

			m_Cube.hoverStarted += OnHoverStarted;
			m_Cube.hoverEnded += OnHoverEnded;

			m_Cube.getDropObject += GetDropObject;
			m_Cube.canDrop += CanDrop;
			m_Cube.receiveDrop += ReceiveDrop;

			var dropZoneRenderer = m_DropZone.GetComponent<Renderer>();
			dropZoneMaterial = U.Material.GetMaterialClone(dropZoneRenderer);
			var color = dropZoneMaterial.color;
			m_DropZoneHighlightAlpha = color.a;
			color.a = 0;
			dropZoneMaterial.color = color;

			m_DropZone.dropHoverStarted += OnDropHoverStarted;
			m_DropZone.dropHoverEnded += OnDropHoverEnded;

			m_DropZone.canDrop = CanDrop;
			m_DropZone.receiveDrop = ReceiveDrop;
			m_DropZone.getDropObject = GetDropObject;
		}

		m_CubeTransform = m_Cube.transform;
		m_DropZoneTransform = m_DropZone.transform;
		m_Text.text = listData.name;

		// HACK: We need to kick the canvasRenderer to update the mesh properly
		m_Text.gameObject.SetActive(false);
		m_Text.gameObject.SetActive(true);

		m_ExpandArrow.gameObject.SetActive(listData.children != null);
		m_Hovering = false;
	}

	public void SetMaterials(Material noClipBackingCube, Material textMaterial, Material expandArrowMaterial)
	{
		m_NoClipBackingCube = noClipBackingCube;
		m_Text.material = textMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
	}

	public void UpdateSelf(float width, int depth, bool expanded, bool selected)
	{
		var cubeScale = m_CubeTransform.localScale;
		cubeScale.x = width;
		m_CubeTransform.localScale = cubeScale;

		var expandArrowTransform = m_ExpandArrow.transform;

		var arrowWidth = expandArrowTransform.localScale.x * 0.5f;
		var halfWidth = width * 0.5f;
		var indent = kIndent * depth;
		const float doubleMargin = kMargin * 2;
		expandArrowTransform.localPosition = new Vector3(kMargin + indent - halfWidth, expandArrowTransform.localPosition.y, 0);

		// Text is next to arrow, with a margin and indent, rotated toward camera
		var textTransform = m_Text.transform;
		m_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width - doubleMargin - indent) * 1 / textTransform.localScale.x);
		textTransform.localPosition = new Vector3(doubleMargin + indent + arrowWidth - halfWidth, textTransform.localPosition.y, 0);

		textTransform.localRotation = U.Camera.LocalRotateTowardCamera(transform.parent.rotation);

		var dropZoneScale = m_DropZoneTransform.localScale;
		dropZoneScale.x = width - indent;
		m_DropZoneTransform.localScale = dropZoneScale;
		var dropZonePosition = m_DropZoneTransform.localPosition;
		dropZonePosition.x = indent * 0.5f;
		m_DropZoneTransform.localPosition = dropZonePosition;

		UpdateArrow(expanded);

		// Set selected/hover/normal color
		if (m_Hovering)
			cubeMaterial.color = m_HoverColor;
		else if (selected)
			cubeMaterial.color = m_SelectedColor;
		else
			cubeMaterial.color = m_NormalColor;
	}

	public void UpdateArrow(bool expanded, bool immediate = false)
	{
		var expandArrowTransform = m_ExpandArrow.transform;
		// Rotate arrow for expand state
		expandArrowTransform.localRotation = Quaternion.Lerp(expandArrowTransform.localRotation,
			Quaternion.AngleAxis(90f, Vector3.right) * (expanded ? Quaternion.AngleAxis(90f, Vector3.back) : Quaternion.identity),
			immediate ? 1f : kExpandArrowRotateSpeed);
	}

	protected override void OnSingleClick(BaseHandle handle, HandleEventData eventData)
	{
		SelectFolder();
		ToggleExpanded(handle, eventData);
	}

	protected override void OnDoubleClick(BaseHandle handle, HandleEventData eventData)
	{
		var row = handle.transform.parent;
		if (row)
		{
			var clone = (GameObject)Instantiate(row.gameObject, row.parent);

			m_DragObject = clone.transform;

			StartCoroutine(Magnetize());

			var graphics = clone.GetComponentsInChildren<Graphic>(true);
			foreach(var graphic in graphics)
			{
				graphic.material = null;
			}

			var item = clone.GetComponent<HierarchyListItem>();
			item.m_Cube.GetComponent<Renderer>().sharedMaterial = m_NoClipBackingCube;

			item.m_DropZone.GetComponent<Renderer>().enabled = false;
		}
	}

	protected override void OnDragging(BaseHandle handle, HandleEventData eventData)
	{
		base.OnDragging(handle, eventData);

		if (m_DragObject)
		{
			var previewOrigin = getPreviewOriginForRayOrigin(eventData.rayOrigin);
			U.Math.LerpTransform(m_DragObject, previewOrigin.position,
				U.Math.ConstrainYawRotation(U.Camera.GetMainCamera().transform.rotation)
				* Quaternion.AngleAxis(90, Vector3.left), m_DragLerp);
		}
	}

	protected override void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (m_DragObject)
			U.Object.Destroy(m_DragObject.gameObject);

		base.OnDragEnded(baseHandle, eventData);
	}

	void ToggleExpanded(BaseHandle handle, HandleEventData eventData)
	{
		toggleExpanded(data.instanceID);
	}

	void SelectFolder()
	{
		selectRow(data.instanceID);
	}

	void OnHoverStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		m_Hovering = true;
	}

	void OnHoverEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		m_Hovering = false;
	}

	void OnDropHoverStarted(BaseHandle handle)
	{
		var color = dropZoneMaterial.color;
		color.a = m_DropZoneHighlightAlpha;
		dropZoneMaterial.color = color;
	}

	void OnDropHoverEnded(BaseHandle handle)
	{
		var color = dropZoneMaterial.color;
		color.a = 0;
		dropZoneMaterial.color = color;
	}

	object GetDropObject(BaseHandle handle)
	{
		return data;
	}

	bool CanDrop(BaseHandle handle, object dropObject)
	{
		var hierarchyData = dropObject as HierarchyData;
		if (hierarchyData == null)
			return false;

		// Dropping on own zone would otherwise move object down
		if (dropObject == data)
			return false;

		if (handle == m_Cube)
			return true;

		if (isExpanded(data.instanceID))
			return true;

		var gameObject = (GameObject)EditorUtility.InstanceIDToObject(data.instanceID);
		var dropGameObject = (GameObject)EditorUtility.InstanceIDToObject(hierarchyData.instanceID);
		var transform = gameObject.transform;
		var dropTransform = dropGameObject.transform;

		var siblings = transform.parent == null && dropTransform.parent == null
			|| transform.parent && dropTransform.parent == transform.parent;

		// Dropping on previous sibling's zone has no effect
		if (siblings && transform.GetSiblingIndex() == dropTransform.GetSiblingIndex() - 1)
			return false;
		
		return true;
	}

	void ReceiveDrop(BaseHandle handle, object dropObject)
	{
		var hierarchyData = dropObject as HierarchyData;
		if (hierarchyData != null)
		{
			var gameObject = (GameObject)EditorUtility.InstanceIDToObject(data.instanceID);
			var dropGameObject = (GameObject)EditorUtility.InstanceIDToObject(hierarchyData.instanceID);
			var transform = gameObject.transform;
			var dropTransform = dropGameObject.transform;

			if (handle == m_Cube)
			{
				dropTransform.SetParent(transform);
				dropTransform.SetAsLastSibling();
				setExpanded(data.instanceID, true);
				selectRow(hierarchyData.instanceID);
			}
			else if (handle == m_DropZone)
			{
				if (isExpanded(data.instanceID))
				{
					dropTransform.SetParent(transform);
					dropTransform.SetAsFirstSibling();
				}
				else if (transform.parent)
				{
					dropTransform.SetParent(transform.parent);
					dropTransform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
				}
				else
				{
					var targetIndex = transform.GetSiblingIndex() + 1;
					if (dropTransform.parent == transform.parent && dropTransform.GetSiblingIndex() < targetIndex)
						targetIndex--;

					dropTransform.SetParent(null);
					dropTransform.SetSiblingIndex(targetIndex);
				}
			}
		}
	}

	void OnDestroy()
	{
		U.Object.Destroy(cubeMaterial);
		U.Object.Destroy(dropZoneMaterial);
	}
}