using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;

public class InspectorWorkspace : Workspace, IPositionPreview, IDroppable, IDropReceiver, ISelectionChanged
{
	const float kScrollMargin = 0.03f;
	public new static readonly Vector3 kDefaultBounds = new Vector3(0.3f, 0.1f, 0.5f);

	[SerializeField]
	GameObject m_ContentPrefab;

	[SerializeField]
	bool m_IsLocked;

	InspectorUI m_InspectorUI;

	Vector3 m_ScrollStart;
	float m_ScrollOffsetStart;

	public PositionPreviewDelegate positionPreview { private get; set; }
	public Func<Transform, Transform> getPreviewOriginForRayOrigin { private get; set; }

	public GetDropReceiverDelegate getCurrentDropReceiver { private get; set; }
	public Func<Transform, object> getCurrentDropObject { private get; set; }
	public Action<Transform, IDropReceiver, GameObject> setCurrentDropReceiver { private get; set; }
	public Action<Transform, object> setCurrentDropObject { private get; set; }

	public override void Setup()
	{
		base.Setup();
		var contentPrefab = U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_InspectorUI = contentPrefab.GetComponent<InspectorUI>();

		var listView = m_InspectorUI.inspectorListView;
		listView.data = new InspectorData[0];
		listView.instantiateUI = instantiateUI;
		listView.getCurrentDropReceiver = getCurrentDropReceiver;
		listView.getCurrentDropObject = getCurrentDropObject;
		listView.setCurrentDropReceiver = setCurrentDropReceiver;
		listView.setCurrentDropObject = setCurrentDropObject;
		listView.positionPreview = positionPreview;
		listView.getPreviewOriginForRayOrigin = getPreviewOriginForRayOrigin;
		listView.setHighlight = setHighlight;
		listView.getIsLocked = GetIsLocked;
		listView.setIsLocked = SetIsLocked;

		var scrollHandle = m_InspectorUI.inspectorScrollHandle;
		scrollHandle.dragStarted += OnScrollDragStarted;
		scrollHandle.dragging += OnScrollDragging;
		scrollHandle.dragEnded += OnScrollDragEnded;
		scrollHandle.hoverStarted += OnScrollHoverStarted;
		scrollHandle.hoverEnded += OnScrollHoverEnded;

		minBounds = kDefaultBounds;
		contentBounds = new Bounds(Vector3.zero, kDefaultBounds);
	}

	void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		m_ScrollOffsetStart = m_InspectorUI.inspectorListView.scrollOffset;

		m_InspectorUI.inspectorListView.OnBeginScrolling();
	}

	void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(eventData);
	}

	void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(eventData);
		m_ScrollOffsetStart = m_InspectorUI.inspectorListView.scrollOffset;
		m_InspectorUI.inspectorListView.OnScrollEnded();
	}

	void Scroll(HandleEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart - Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		m_InspectorUI.inspectorListView.scrollOffset = scrollOffset;
	}

	void OnScrollHoverStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		setHighlight(handle.gameObject, true);
	}

	void OnScrollHoverEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		setHighlight(handle.gameObject, false);
	}

	public void OnSelectionChanged()
	{
		if (m_IsLocked)
			return;

		if (Selection.activeObject == null)
		{
			m_InspectorUI.inspectorListView.data = new InspectorData[0];
			return;
		}
		var inspectorData = new List<InspectorData>();

		var objectChildren = new List<InspectorData>();

		if (Selection.activeGameObject)
		{
			foreach (var component in Selection.activeGameObject.GetComponents<Component>())
			{
				var obj = new SerializedObject(component);

				var componentChildren = new List<InspectorData>();

				var property = obj.GetIterator();
				while (property.NextVisible(true))
				{
					if (property.depth == 0)
						componentChildren.Add(SerializedPropertyToPropertyData(property, obj));
				}

				var componentData = new InspectorData("InspectorComponentItem", obj, componentChildren.ToArray()) { expanded = true };
				objectChildren.Add(componentData);
			}
		}

		var objectData = new InspectorData("InspectorHeaderItem", new SerializedObject(Selection.activeObject), objectChildren.ToArray()) { expanded = true };
		inspectorData.Add(objectData);

		m_InspectorUI.inspectorListView.data = inspectorData.ToArray();
	}

	PropertyData SerializedPropertyToPropertyData(SerializedProperty property, SerializedObject obj)
	{
		string template;
		switch (property.propertyType)
		{
			case SerializedPropertyType.Vector2:
			case SerializedPropertyType.Vector3:
			case SerializedPropertyType.Vector4:
			case SerializedPropertyType.Quaternion:
				template = "InspectorVectorItem";
				break;
			case SerializedPropertyType.Integer:
				goto case SerializedPropertyType.Float;
			case SerializedPropertyType.Float:
				template = "InspectorNumberItem";
				break;
			case SerializedPropertyType.Character:
			case SerializedPropertyType.String:
				template = "InspectorStringItem";
				break;
			case SerializedPropertyType.Bounds:
				template = "InspectorBoundsItem";
				break;
			case SerializedPropertyType.Boolean:
				template = "InspectorBoolItem";
				break;
			case SerializedPropertyType.ObjectReference:
				template = "InspectorObjectFieldItem";
				break;
			case SerializedPropertyType.Color:
				template = "InspectorColorItem";
				break;
			case SerializedPropertyType.Rect:
				template = "InspectorRectItem";
				break;
			case SerializedPropertyType.LayerMask:
			case SerializedPropertyType.Enum:
				template = "InspectorDropDownItem";
				break;
			case SerializedPropertyType.Generic:
				return GenericProperty(property, obj);
			default:
				template = "InspectorUnimplementedItem";
				break;
		}

		return new PropertyData(template, obj, null, property.Copy());
	}

	PropertyData GenericProperty(SerializedProperty property, SerializedObject obj)
	{
		var propertyData = property.isArray
			? new PropertyData("InspectorArrayHeaderItem", obj, null, property.Copy())
			: new PropertyData("InspectorGenericItem", obj, null, property.Copy()) {expanded = true};
		
		propertyData.SetChildren(GetChildProperties(propertyData, property, obj));

		return propertyData;
	}

	InspectorData[] GetChildProperties(PropertyData parent, SerializedProperty property, SerializedObject obj)
	{
		var children = new List<InspectorData>();
		var iteratorProperty = property.Copy();
		while (iteratorProperty.NextVisible(true))
		{
			if (iteratorProperty.depth == 0)
				break;

			switch (iteratorProperty.propertyType)
			{
				case SerializedPropertyType.ArraySize:
					children.Add(new PropertyData("InspectorNumberItem", obj, null, iteratorProperty.Copy(), () =>
					{
						parent.SetChildren(GetChildProperties(parent, parent.property.Copy(), obj));
					}));
					break;
				default:
					children.Add(SerializedPropertyToPropertyData(iteratorProperty, obj));
					break;
			}
		}
		return children.ToArray();
	}

	protected override void OnBoundsChanged()
	{
		var size = contentBounds.size;
		var inspectorScrollHandleTransform = m_InspectorUI.inspectorScrollHandle.transform;
		inspectorScrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin, inspectorScrollHandleTransform.localScale.y, size.z + kScrollMargin);

		var inspectorListView = m_InspectorUI.inspectorListView;
		var bounds = contentBounds;
		size.y = float.MaxValue; // Add height for dropdowns
		bounds.size = size;
		inspectorListView.bounds = bounds;

		var inspectorPanel = m_InspectorUI.inspectorPanel;
		inspectorPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
		inspectorPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z);
	}

	public bool TestDrop(GameObject target, object droppedObject)
	{
		return false;
	}

	public bool ReceiveDrop(GameObject target, object droppedObject)
	{
		return false;
	}

	bool GetIsLocked()
	{
		return m_IsLocked;
	}

	void SetIsLocked(bool isLocked)
	{
		m_IsLocked = isLocked;
	}
}