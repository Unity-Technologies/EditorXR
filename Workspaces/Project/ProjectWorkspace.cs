using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;

public class ProjectWorkspace : Workspace, IPlaceObjects, IPreview
{
	const float kLeftPaneRatio = 0.3333333f; // Size of left pane relative to workspace bounds
	const float kPaneMargin = 0.01f;
	const float kPanelMargin = 0.01f;
	const float kScrollMargin = 0.03f;
	const float kYBounds = 0.2f;

	const float kMinScale = 0.03f;
	const float kMaxScale = 0.2f;

	[SerializeField]
	GameObject m_ContentPrefab;

	[SerializeField]
	GameObject m_SliderPrefab;

	[SerializeField]
	GameObject m_FilterPrefab;

	ProjectUI m_ProjectUI;
	FilterUI m_FilterUI;

	Vector3 m_ScrollStart;
	float m_ScrollOffsetStart;

	public Action<Transform, Vector3> placeObject { private get; set; }

	public Func<Transform, Transform> getPreviewOriginForRayOrigin { private get; set; }
	public PreviewDelegate preview { private get; set; }

	public override void Setup()
	{
		// Initial bounds must be set before the base.Setup() is called
		minBounds = new Vector3(kMinBounds.x, kMinBounds.y, 0.5f);
		m_CustomStartingBounds = minBounds;

		base.Setup();

		topPanelDividerOffset = -0.2875f; // enable & position the top-divider(mask) slightly to the left of workspace center
		dynamicFaceAdjustment = true;

		var contentPrefab = U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_ProjectUI = contentPrefab.GetComponent<ProjectUI>();

		var filterPrefab = U.Object.Instantiate(m_FilterPrefab, m_WorkspaceUI.frontPanel, false);
		m_FilterUI = filterPrefab.GetComponent<FilterUI>();

		var sliderPrefab = U.Object.Instantiate(m_SliderPrefab, m_WorkspaceUI.frontPanel, false);
		var zoomSlider = sliderPrefab.GetComponent<ZoomSliderUI>();
		zoomSlider.zoomSlider.minValue = kMinScale;
		zoomSlider.zoomSlider.maxValue = kMaxScale;
		zoomSlider.zoomSlider.value = m_ProjectUI.assetListView.scaleFactor;
		zoomSlider.sliding += Scale;

		m_ProjectUI.folderListView.selectFolder = SelectFolder;

		var assetListView = m_ProjectUI.assetListView;
		assetListView.testFilter = TestFilter;
		assetListView.placeObject = placeObject;
		assetListView.getPreviewOriginForRayOrigin = getPreviewOriginForRayOrigin;
		assetListView.preview = preview;

#if UNITY_EDITOR
		EditorApplication.projectWindowChanged += SetupFolderList;
		SetupFolderList();
#else
		Debug.LogWarning("Project workspace does not work in builds");
		return;
#endif
		var scrollHandles = new[]
		{
			m_ProjectUI.folderScrollHandle,
			m_ProjectUI.assetScrollHandle
		};
		foreach (var handle in scrollHandles)
		{
			// Scroll Handle shouldn't move on bounds change
			handle.transform.parent = m_WorkspaceUI.sceneContainer;

			handle.dragStarted += OnScrollDragStarted;
			handle.dragging += OnScrollDragging;
			handle.dragEnded += OnScrollDragEnded;
			handle.hoverStarted += OnScrollHoverStarted;
			handle.hoverEnded += OnScrollHoverEnded;
		}

		// Propagate initial bounds
		OnBoundsChanged();
	}

	protected override void OnBoundsChanged()
	{
		const float depthCompensation = 0.1375f;

		Bounds bounds = contentBounds;
		Vector3 size = bounds.size;
		size.x -= kPaneMargin * 2;
		size.x *= kLeftPaneRatio;
		size.y = kYBounds;
		size.z = size.z - depthCompensation;
		bounds.size = size;
		bounds.center = Vector3.zero;

		var halfScrollMargin = kScrollMargin * 0.5f;
		var doubleScrollMargin = kScrollMargin * 2;
		var xOffset = (contentBounds.size.x - size.x + kPaneMargin) * -0.5f;
		var folderScrollHandleXPositionOffset = 0.025f;
		var folderScrollHandleXScaleOffset = 0.015f;

		var folderScrollHandleTransform = m_ProjectUI.folderScrollHandle.transform;
		folderScrollHandleTransform.localPosition = new Vector3(xOffset - halfScrollMargin + folderScrollHandleXPositionOffset, -folderScrollHandleTransform.localScale.y * 0.5f, 0);
		folderScrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin + folderScrollHandleXScaleOffset, folderScrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

		var folderListView = m_ProjectUI.folderListView;
		folderListView.bounds = bounds;
		folderListView.PreCompute(); // Compute item size
		folderListView.transform.localPosition = new Vector3(xOffset, folderListView.itemSize.y * 0.5f, 0);

		var folderPanel = m_ProjectUI.folderPanel;
		folderPanel.transform.localPosition = xOffset * Vector3.right;
		folderPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + kPanelMargin);
		folderPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + kPanelMargin);

		size = contentBounds.size;
		size.x -= kPaneMargin * 2;
		size.x *= 1 - kLeftPaneRatio;
		size.z = size.z - depthCompensation;
		bounds.size = size;

		xOffset = (contentBounds.size.x - size.x + kPaneMargin) * 0.5f;

		var assetScrollHandleTransform = m_ProjectUI.assetScrollHandle.transform;
		assetScrollHandleTransform.localPosition = new Vector3(xOffset + halfScrollMargin, -assetScrollHandleTransform.localScale.y * 0.5f);
		assetScrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin, assetScrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

		var assetListView = m_ProjectUI.assetListView;
		assetListView.bounds = bounds;
		assetListView.PreCompute(); // Compute item size
		assetListView.transform.localPosition = Vector3.right * xOffset;

		var assetPanel = m_ProjectUI.assetPanel;
		assetPanel.transform.localPosition = xOffset * Vector3.right;
		assetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + kPanelMargin);
		assetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + kPanelMargin);
	}

	void SelectFolder(FolderData data)
	{
		m_ProjectUI.folderListView.ClearSelected();
		data.selected = true;
		m_ProjectUI.assetListView.data = data.assets;
	}

	void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		if (handle == m_ProjectUI.folderScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.folderListView.scrollOffset;
			m_ProjectUI.folderListView.OnBeginScrolling();
		}
		else if (handle == m_ProjectUI.assetScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.assetListView.scrollOffset;
			m_ProjectUI.assetListView.OnBeginScrolling();
		}
	}

	void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
	}

	void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
		if (handle == m_ProjectUI.folderScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.folderListView.scrollOffset;
			m_ProjectUI.folderListView.OnScrollEnded();
		}
		else if (handle == m_ProjectUI.assetScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.assetListView.scrollOffset;
			m_ProjectUI.assetListView.OnScrollEnded();
		}
	}

	void Scroll(BaseHandle handle, HandleEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart + Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		if (handle == m_ProjectUI.folderScrollHandle)
			m_ProjectUI.folderListView.scrollOffset = scrollOffset;
		else if (handle == m_ProjectUI.assetScrollHandle)
			m_ProjectUI.assetListView.scrollOffset = scrollOffset;
	}

	void OnScrollHoverStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		setHighlight(handle.gameObject, true);
	}

	void OnScrollHoverEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		setHighlight(handle.gameObject, false);
	}

	void Scale(float value)
	{
		m_ProjectUI.assetListView.scaleFactor = value;
	}

	bool TestFilter(string type)
	{
		return FilterUI.TestFilter(m_FilterUI.searchQuery, type);
	}

#if UNITY_EDITOR
	void SetupFolderList()
	{
		var assetTypes = new HashSet<string>();
		var hasNext = true;
		var rootFolder = CreateFolderData(assetTypes, ref hasNext);
		rootFolder.expanded = true;
		m_ProjectUI.folderListView.data = new[] { rootFolder };

		SelectFolder(rootFolder);
		m_FilterUI.filterTypes = assetTypes.ToList();
	}

	FolderData CreateFolderData(HashSet<string> assetTypes, ref bool hasNext, HierarchyProperty hp = null)
	{
		if (hp == null)
		{
			hp = new HierarchyProperty(HierarchyType.Assets);
			hp.SetSearchFilter("t:object", 0);
		}
		var name = hp.name;
		var depth = hp.depth;
		var folderList = new List<FolderData>();
		var assetList = new List<AssetData>();
		if (hasNext)
		{
			hasNext = hp.Next(null);
			while (hasNext && hp.depth > depth)
			{
				if (hp.isFolder)
					folderList.Add(CreateFolderData(assetTypes, ref hasNext, hp));
				else if (hp.isMainRepresentation) // Ignore sub-assets (mixer children, terrain splats, etc.)
					assetList.Add(CreateAssetData(assetTypes, hp));
				if(hasNext)
					hasNext = hp.Next(null);
			}
			if (hasNext)
				hp.Previous(null);
		}
		return new FolderData(name, folderList.Count > 0 ? folderList.ToArray() : null, assetList.ToArray());
	}

	AssetData CreateAssetData(HashSet<string> assetTypes, HierarchyProperty hp)
	{
		var type = hp.pptrValue.GetType().Name;
		switch (type)
		{
			case "GameObject":
				switch (PrefabUtility.GetPrefabType(EditorUtility.InstanceIDToObject(hp.instanceID)))
				{
					case PrefabType.ModelPrefab:
						type = "Model";
						break;
					default:
						type = "Prefab";
						break;
				}
				break;
			case "MonoScript":
				type = "Script";
				break;
			case "SceneAsset":
				type = "Scene";
				break;
			case "AudioMixerController":
				type = "AudioMixer";
				break;
		}
		assetTypes.Add(type);
		return new AssetData(hp.name, hp.instanceID, hp.icon, type);
	}


	protected override void OnDestroy()
	{
		EditorApplication.projectWindowChanged -= SetupFolderList;
		base.OnDestroy();
	}
#endif
}