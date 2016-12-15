using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;

[MainMenuItem("Inspector", "Workspaces", "View and edit GameObject properties")]
public class InspectorWorkspace : Workspace, ISelectionChanged
{
	public new static readonly Vector3 kDefaultBounds = new Vector3(0.3f, 0.1f, 0.5f);

	[SerializeField]
	GameObject m_ContentPrefab;

	[SerializeField]
	GameObject m_LockPrefab;

	InspectorUI m_InspectorUI;
	GameObject m_SelectedObject;
	LockUI m_LockUI;

	bool m_Scrolling;
	Vector3 m_ScrollStart;
	float m_ScrollOffsetStart;

	bool m_IsLocked;

#if UNITY_EDITOR
	public override void Setup()
	{
		// Initial bounds must be set before the base.Setup() is called
		minBounds = new Vector3(0.375f, kMinBounds.y, 0.3f);
		m_CustomStartingBounds = new Vector3(0.375f, kMinBounds.y, 0.6f);

		base.Setup();
		var contentPrefab = U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_InspectorUI = contentPrefab.GetComponent<InspectorUI>();
		
		m_LockUI = U.Object.Instantiate(m_LockPrefab, m_WorkspaceUI.frontPanel, false).GetComponentInChildren<LockUI>();
		connectInterfaces(m_LockUI);
		m_LockUI.lockButtonPressed += SetIsLocked;

		var listView = m_InspectorUI.inspectorListView;
		connectInterfaces(listView);
		listView.data = new List<InspectorData>();
		listView.arraySizeChanged += OnArraySizeChanged;

		var scrollHandle = m_InspectorUI.inspectorScrollHandle;
		scrollHandle.dragStarted += OnScrollDragStarted;
		scrollHandle.dragging += OnScrollDragging;
		scrollHandle.dragEnded += OnScrollDragEnded;
		scrollHandle.hoverStarted += OnScrollHoverStarted;
		scrollHandle.hoverEnded += OnScrollHoverEnded;

		contentBounds = new Bounds(Vector3.zero, m_CustomStartingBounds.Value);

		var scrollHandleTransform = m_InspectorUI.inspectorScrollHandle.transform;
		scrollHandleTransform.SetParent(m_WorkspaceUI.topFaceContainer);
		scrollHandleTransform.localScale = new Vector3(1.03f, 0.02f, 1.02f); // Extra space for scrolling
		scrollHandleTransform.localPosition = new Vector3(0f, -0.01f, 0f); // Offset from content for collision purposes

		if (Selection.activeGameObject)
			OnSelectionChanged();
	}

	void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_Scrolling = true;

		m_WorkspaceUI.topHighlight.visible = true;
		m_WorkspaceUI.amplifyTopHighlight = false;

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
		m_Scrolling = false;

		m_WorkspaceUI.topHighlight.visible = false;

		Scroll(eventData);
		m_ScrollOffsetStart = m_InspectorUI.inspectorListView.scrollOffset;
		m_InspectorUI.inspectorListView.OnScrollEnded();
	}

	void OnScrollHoverStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		if (!m_Scrolling)
		{
			m_WorkspaceUI.topHighlight.visible = true;
			m_WorkspaceUI.amplifyTopHighlight = true;
		}
	}

	void OnScrollHoverEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		if (!m_Scrolling)
		{
			m_WorkspaceUI.topHighlight.visible = false;
			m_WorkspaceUI.amplifyTopHighlight = false;
		}
	}

	void Scroll(HandleEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart - Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		m_InspectorUI.inspectorListView.scrollOffset = scrollOffset;
	}

	public void OnSelectionChanged()
	{
		if (m_IsLocked)
			return;

		if (Selection.activeGameObject == m_SelectedObject)
			return;

		if (Selection.activeGameObject == null)
		{
			m_InspectorUI.inspectorListView.data = new List<InspectorData>();
			m_SelectedObject = null;
			return;
		}

		var inspectorData = new List<InspectorData>();
		var objectChildren = new List<InspectorData>();

		if (Selection.activeGameObject)
		{
			m_SelectedObject = Selection.activeGameObject;
			foreach (var component in m_SelectedObject.GetComponents<Component>())
			{
				var obj = new SerializedObject(component);

				var componentChildren = new List<InspectorData>();

				var property = obj.GetIterator();
				while (property.NextVisible(true))
				{
					if (property.depth == 0)
						componentChildren.Add(SerializedPropertyToPropertyData(property, obj));
				}

				var componentData = new InspectorData("InspectorComponentItem", obj, componentChildren);
				objectChildren.Add(componentData);
			}
		}

		var objectData = new InspectorData("InspectorHeaderItem", new SerializedObject(Selection.activeObject), objectChildren);
		inspectorData.Add(objectData);

		m_InspectorUI.inspectorListView.data = inspectorData;
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
		var children = GetSubProperties(property, obj);

		var propertyData = property.isArray
			? new PropertyData("InspectorArrayHeaderItem", obj, children, property.Copy())
			: new PropertyData("InspectorGenericItem", obj, children, property.Copy());

		propertyData.childrenChanging += m_InspectorUI.inspectorListView.OnBeforeChildrenChanged;

		return propertyData;
	}

	List<InspectorData> GetSubProperties(SerializedProperty property, SerializedObject obj)
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
					children.Add(new PropertyData("InspectorNumberItem", obj, null, iteratorProperty.Copy()));
					break;
				default:
					children.Add(SerializedPropertyToPropertyData(iteratorProperty, obj));
					break;
			}
		}
		return children;
	}

	void OnArraySizeChanged(List<InspectorData> data, PropertyData element)
	{
		foreach (var d in data)
		{
			if (FindElementAndUpdateParent(d, element))
				break;
		}
	}

	bool FindElementAndUpdateParent(InspectorData parent, PropertyData element)
	{
		if (parent.children != null)
		{
			foreach (var child in parent.children)
			{
				if (child == element)
				{
					var propertyData = (PropertyData)parent;
					propertyData.children = GetSubProperties(propertyData.property.Copy(), propertyData.serializedObject);
					return true;
				}

				if (FindElementAndUpdateParent(child, element))
					return true;
			}
		}

		return false;
	}

	protected override void OnBoundsChanged()
	{
		var size = contentBounds.size;
		var inspectorListView = m_InspectorUI.inspectorListView;
		var bounds = contentBounds;
		size.y = float.MaxValue; // Add height for dropdowns
		size.x -= 0.04f; // Shrink the content width, so that there is space allowed to grab and scroll
		size.z -= 0.15f; // Reduce the height of the inspector contents as to fit within the bounds of the workspace
		bounds.size = size;
		inspectorListView.bounds = bounds;

		var inspectorPanel = m_InspectorUI.inspectorPanel;
		inspectorPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
		inspectorPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z);
	}

	void SetIsLocked()
	{
		m_IsLocked = !m_IsLocked;
		m_LockUI.UpdateIcon(m_IsLocked);

		if (!m_IsLocked)
			OnSelectionChanged();
	}
#else
	public void OnSelectionChanged()
	{
	}
#endif
}