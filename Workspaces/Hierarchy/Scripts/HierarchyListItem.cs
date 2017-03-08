using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.UI;

sealed class HierarchyListItem : DraggableListItem<HierarchyData, int>
{
	const float kMargin = 0.01f;
	const float kIndent = 0.02f;

	const float kExpandArrowRotateSpeed = 0.4f;

	[SerializeField]
	Text m_Text;

	[SerializeField]
	BaseHandle m_Cube;

	[SerializeField]
	BaseHandle m_ExpandArrow;

	[SerializeField]
	BaseHandle m_DropZone;

	[SerializeField]
	Material m_NoClipExpandArrow;

	[SerializeField]
	Material m_NoClipBackingCube;

	[SerializeField]
	Color m_HoverColor;

	[SerializeField]
	Color m_SelectedColor;

	[Tooltip("The fraction of the cube height to use for stacking grabbed rows")]
	[SerializeField]
	float m_StackingFraction = 0.3f;

	Color m_NormalColor;
	bool m_Hovering;
	Transform m_CubeTransform;
	Transform m_DropZoneTransform;

	float m_DropZoneHighlightAlpha;

	bool m_Settling;

	readonly Dictionary<Graphic, Material> m_OldMaterials = new Dictionary<Graphic, Material>();
	readonly List<HierarchyListItem> m_VisibleChildren = new List<HierarchyListItem>();

	Material m_ExpandArrowMaterial;
	public Material cubeMaterial { get; private set; }
	public Material dropZoneMaterial { get; private set; }

	public Action<int> toggleExpanded { private get; set; }
	public Action<int, bool> setExpanded { private get; set; }
	public Action<int> selectRow { private get; set; }
	public Action<Action> startSettling { private get; set; }
	public Action endSettling { private get; set; }

	public Func<int, bool> isExpanded { private get; set; }
	public Action<int, bool> setRowGrabbed { private get; set; }
	public Func<int, HierarchyListItem> getListItem { private get; set; }

	protected override bool singleClickDrag { get { return false; } }

	public bool isStillSettling
	{
		get { return m_DragLerp < 1; }
	}

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

		m_Hovering = false;
	}

	public void SetMaterials(Material textMaterial, Material expandArrowMaterial)
	{
		m_Text.material = textMaterial;
		m_ExpandArrowMaterial = expandArrowMaterial;
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

		m_Text.text = m_Settling ? "Settling" : data.name;

		// Set selected/hover/normal color
		if (m_Hovering && !m_Settling)
			cubeMaterial.color = m_HoverColor;
		else if (selected)
			cubeMaterial.color = m_SelectedColor;
		else
			cubeMaterial.color = m_NormalColor;
	}

	public void UpdateArrow(bool expanded, bool immediate = false)
	{
		m_ExpandArrow.gameObject.SetActive(data.children != null);
		var expandArrowTransform = m_ExpandArrow.transform;
		// Rotate arrow for expand state
		expandArrowTransform.localRotation = Quaternion.Lerp(expandArrowTransform.localRotation,
			Quaternion.AngleAxis(90f, Vector3.right) * (expanded ? Quaternion.AngleAxis(90f, Vector3.back) : Quaternion.identity),
			immediate ? 1f : kExpandArrowRotateSpeed);
	}

	public void OnStartSettling()
	{
		m_Settling = true;
	}

	public void OnEndSettling()
	{
		m_Settling = false;
	}

	protected override void OnSingleClick(BaseHandle handle, HandleEventData eventData)
	{
		if (m_Settling)
			return;

		SelectFolder();
		ToggleExpanded(handle, eventData);
	}

	protected override void OnDoubleClick(BaseHandle handle, HandleEventData eventData)
	{
		var row = handle.transform.parent;
		if (row)
		{
			m_DragObject = transform;

			StartCoroutine(Magnetize());

			m_VisibleChildren.Clear();
			OnGrabRecursive(m_VisibleChildren);
			startSettling(null);
		}
	}

	void OnGrabRecursive(List<HierarchyListItem> visibleChildren)
	{
		m_OldMaterials.Clear();
		var graphics = GetComponentsInChildren<Graphic>(true);
		foreach (var graphic in graphics)
		{
			m_OldMaterials[graphic] = graphic.material;
			graphic.material = null;
		}

		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = m_NoClipExpandArrow;
		m_Cube.GetComponent<Renderer>().sharedMaterial = m_NoClipBackingCube;
		m_Text.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);
		
		m_DropZone.gameObject.SetActive(false);
		m_Cube.GetComponent<Collider>().enabled = false;

		setRowGrabbed(data.index, true);

		if (data.children != null)
		{
			foreach (var child in data.children)
			{
				var item = getListItem(child.index);
				if (item)
				{
					visibleChildren.Add(item);
					item.OnGrabRecursive(visibleChildren);
				}
			}
		}
	}

	protected override void OnDragging(BaseHandle handle, HandleEventData eventData)
	{
		base.OnDragging(handle, eventData);

		if (m_DragObject)
		{
			var previewOrigin = getPreviewOriginForRayOrigin(eventData.rayOrigin);
			MagnetizeTransform(previewOrigin, m_DragObject);
			var offset = 0f;
			foreach (var child in m_VisibleChildren)
			{
				offset += m_Cube.GetComponent<Renderer>().bounds.size.y * m_StackingFraction;
				MagnetizeTransform(previewOrigin, child.transform, offset);
			}
		}
	}

	void MagnetizeTransform(Transform previewOrigin, Transform transform, float offset = 0)
	{
		var rotation = U.Math.ConstrainYawRotation(U.Camera.GetMainCamera().transform.rotation)
			* Quaternion.AngleAxis(90, Vector3.left);
		var offsetDirection = rotation * Vector3.one;
		U.Math.LerpTransform(transform, previewOrigin.position - offsetDirection * offset, rotation, m_DragLerp);
	}

	protected override void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (m_DragObject)
		{
			OnDragEndRecursive();

			startSettling(OnDragEndAfterSettling);
		}

		base.OnDragEnded(baseHandle, eventData);
	}

	void OnDragEndRecursive()
	{
		m_DragLerp = 0;
		// OnHierarchyChanged doesn't happen until next frame--delay un-grab so the object doesn't start moving to the wrong spot
		EditorApplication.delayCall += () => 
		{
			setRowGrabbed(data.index, false);
		};

		foreach (var child in m_VisibleChildren)
		{
			child.OnDragEndRecursive();
		}
	}

	void OnDragEndAfterSettling()
	{
		SetClipMaterialsAfterSettling();
		foreach (var child in m_VisibleChildren)
		{
			child.SetClipMaterialsAfterSettling();
		}
	}

	void SetClipMaterialsAfterSettling()
	{
		foreach (var kvp in m_OldMaterials)
		{
			kvp.Key.material = kvp.Value;
		}

		m_Cube.GetComponent<Renderer>().sharedMaterial = cubeMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = m_ExpandArrowMaterial;
		m_DropZone.gameObject.SetActive(true);
		m_Cube.GetComponent<Collider>().enabled = true;
		m_Hovering = false;
	}

	void ToggleExpanded(BaseHandle handle, HandleEventData eventData)
	{
		toggleExpanded(data.index);
	}

	void SelectFolder()
	{
		selectRow(data.index);
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
		return m_DragObject ? data : null;
	}

	bool CanDrop(BaseHandle handle, object dropObject)
	{
		var dropData = dropObject as HierarchyData;
		if (dropData == null)
			return false;

		// Dropping on own zone would otherwise move object down
		if (dropObject == data)
			return false;

		if (handle == m_Cube)
			return true;

		var index = data.index;
		if (isExpanded(index))
			return true;

		var gameObject = (GameObject)EditorUtility.InstanceIDToObject(index);
		var dropGameObject = (GameObject)EditorUtility.InstanceIDToObject(dropData.index);
		var transform = gameObject.transform;
		var dropTransform = dropGameObject.transform;

		var siblings = (transform.parent == null && dropTransform.parent == null)
			|| (transform.parent && dropTransform.parent == transform.parent);

		// Dropping on previous sibling's zone has no effect
		if (siblings && transform.GetSiblingIndex() == dropTransform.GetSiblingIndex() - 1)
			return false;
		
		return true;
	}

	void ReceiveDrop(BaseHandle handle, object dropObject)
	{
		if (m_Settling)
			return;

		var dropData = dropObject as HierarchyData;
		if (dropData != null)
		{
			var thisIndex = data.index;
			var dropIndex = dropData.index;
			var gameObject = (GameObject)EditorUtility.InstanceIDToObject(thisIndex);
			var dropGameObject = (GameObject)EditorUtility.InstanceIDToObject(dropIndex);
			var transform = gameObject.transform;
			var dropTransform = dropGameObject.transform;

			if (handle == m_Cube)
			{
				dropTransform.SetParent(transform);
				dropTransform.SetAsLastSibling();

				EditorApplication.delayCall += () => { setExpanded(thisIndex, true); };
			}
			else if (handle == m_DropZone)
			{
				if (isExpanded(thisIndex))
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
			else
			{
				Debug.Log("huh??");
			}
		}
		else
		{
			Debug.Log("huh??");
		}
	}

	void OnDestroy()
	{
		U.Object.Destroy(cubeMaterial);
		U.Object.Destroy(dropZoneMaterial);
	}
}