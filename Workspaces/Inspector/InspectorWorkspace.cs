using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;

public class InspectorWorkspace : Workspace
{
	private const float kScrollMargin = 0.03f;
	public new static readonly Vector3 kDefaultBounds = new Vector3(0.3f, 0.1f, 0.5f);

	[SerializeField]
	private GameObject m_ContentPrefab;

	private InspectorUI m_InspectorUI;

	private Vector3 m_ScrollStart;
	private float m_ScrollOffsetStart;

	public override void Setup()
	{
		base.Setup();
		var contentPrefab = U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_InspectorUI = contentPrefab.GetComponent<InspectorUI>();
		m_InspectorUI.inspectorListView.data = new InspectorData[0];

		var scrollHandle = m_InspectorUI.inspectorScrollHandle;

		scrollHandle.dragStarted += OnScrollDragStarted;
		scrollHandle.dragging += OnScrollDragging;
		scrollHandle.dragEnded += OnScrollDragEnded;
		scrollHandle.hoverStarted += OnScrollHoverStarted;
		scrollHandle.hoverEnded += OnScrollHoverEnded;

		Selection.selectionChanged += OnSelectionChanged;

		minBounds = kDefaultBounds;
		contentBounds = new Bounds(Vector3.zero, kDefaultBounds);
	}

	private void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		m_ScrollOffsetStart = m_InspectorUI.inspectorListView.scrollOffset;

		m_InspectorUI.inspectorListView.OnBeginScrolling();
	}

	private void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
	}

	private void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
			m_ScrollOffsetStart = m_InspectorUI.inspectorListView.scrollOffset;
		m_InspectorUI.inspectorListView.OnScrollEnded();
	}

	private void Scroll(BaseHandle handle, HandleEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart + Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		m_InspectorUI.inspectorListView.scrollOffset = scrollOffset;
	}

	private void OnScrollHoverStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		setHighlight(handle.gameObject, true);
	}

	private void OnScrollHoverEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		setHighlight(handle.gameObject, false);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Selection.selectionChanged -= OnSelectionChanged;
	}

	private void OnSelectionChanged()
	{
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

				var iterator = obj.GetIterator();
				while (iterator.NextVisible(true))
				{
					if (iterator.depth == 0)
					{
						var canExpand = false;
						var template = "InspectorItem";
						var children = new InspectorData[0];
						switch (iterator.propertyType)
						{
							case SerializedPropertyType.Vector2:
								goto case SerializedPropertyType.Quaternion;
							case SerializedPropertyType.Vector3:
								goto case SerializedPropertyType.Quaternion;
							case SerializedPropertyType.Vector4:
								goto case SerializedPropertyType.Quaternion;
							case SerializedPropertyType.Quaternion:
								template = "InspectorVectorItem";
								break;
						}
						componentChildren.Add(new PropertyData(template, obj, children, iterator.Copy(), canExpand));
					}
				}
				var componentData = new InspectorData("InspectorComponentItem", obj, componentChildren.ToArray()) { expanded = true };
				objectChildren.Add(componentData);
			}
		}

		var objectData = new InspectorData("InspectorHeaderItem", new SerializedObject(Selection.activeObject), objectChildren.ToArray()) { expanded = true };
		inspectorData.Add(objectData);

		m_InspectorUI.inspectorListView.data = inspectorData.ToArray();
	}

	protected override void OnBoundsChanged()
	{
		var size = contentBounds.size;
		var inspectorScrollHandleTransform = m_InspectorUI.inspectorScrollHandle.transform;
		inspectorScrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin, inspectorScrollHandleTransform.localScale.y, size.z + kScrollMargin);

		var inspectorListView = m_InspectorUI.inspectorListView;
		var bounds = contentBounds;
		bounds.size = (inspectorListView.transform.localRotation * bounds.size).Abs();
		inspectorListView.bounds = bounds;
		inspectorListView.PreCompute(); // Compute item size
		inspectorListView.transform.localPosition = inspectorListView.itemSize.z * Vector3.up;

		var inspectorPanel = m_InspectorUI.inspectorPanel;
		inspectorPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
		inspectorPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z);
	}
}