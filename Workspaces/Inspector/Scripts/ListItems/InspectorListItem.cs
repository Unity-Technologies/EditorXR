using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;
using InputField = UnityEngine.Experimental.EditorVR.UI.InputField;

public abstract class InspectorListItem : DraggableListItem<InspectorData>, ISetHighlight
{
	const float kIndent = 0.02f;

	static readonly Quaternion kPreviewRotation = Quaternion.AngleAxis(90, Vector3.right);

	protected CuboidLayout m_CuboidLayout;

	protected InputField[] m_InputFields;

	protected InputField m_ClickedField;
	protected int m_ClickCount;

	[SerializeField]
	BaseHandle m_Cube;

	[SerializeField]
	RectTransform m_UIContainer;

	ClipText[] m_ClipTexts;

	Material m_NoClipBackingCube;

	bool m_Setup;

	readonly Dictionary<Transform, Vector3> m_DragStarts = new Dictionary<Transform, Vector3>();

	float m_LastClickTime;
	bool m_SelectIsHeld;
	float m_DragDistance;

	public bool setup { get; set; }

	public Action<GameObject, bool> setHighlight { private get; set; }

	public Action<InspectorData> toggleExpanded { private get; set; }

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
		if (m_CuboidLayout)
			m_CuboidLayout.UpdateObjects();

		var handles = GetComponentsInChildren<BaseHandle>(true);
		foreach (var handle in handles)
		{
			// Ignore m_Cube for now (will be used for Reset action)
			if (handle.Equals(m_Cube))
				continue;

			// Toggles can't be dragged
			if (handle.transform.parent.GetComponentInChildren<Toggle>())
				continue;

			handle.dragStarted += OnDragStarted;
			handle.dragging += OnDragging;
			handle.dragEnded += OnDragEnded;

			handle.dropHoverStarted += OnDropHoverStarted;
			handle.dropHoverEnded += OnDropHoverEnded;

			handle.canDrop = CanDrop;
			handle.receiveDrop = ReceiveDrop;
			handle.getDropObject = GetDropObject;
		}

		m_InputFields = GetComponentsInChildren<InputField>(true);
	}

	public virtual void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material uiMaterial, Material textMaterial, Material noClipBackingCube, Material[] highlightMaterials)
	{
		m_NoClipBackingCube = noClipBackingCube;

		m_Cube.GetComponent<Renderer>().sharedMaterial = rowMaterial;

		var cuboidLayouts = GetComponentsInChildren<CuboidLayout>(true);
		foreach (var cuboidLayout in cuboidLayouts)
		{
			cuboidLayout.SetMaterials(backingCubeMaterial, highlightMaterials);
		}

		var workspaceButtons = GetComponentsInChildren<WorkspaceButton>(true);
		foreach (var button in workspaceButtons)
		{
			button.buttonMeshRenderer.sharedMaterials = highlightMaterials;
		}

		var graphics = GetComponentsInChildren<Graphic>(true);
		foreach (var graphic in graphics)
		{
			graphic.material = uiMaterial;
		}

		// Texts need a specific shader
		var texts = GetComponentsInChildren<Text>(true);
		foreach (var text in texts)
		{
			text.material = textMaterial;
		}

		// Don't clip masks
		var masks = GetComponentsInChildren<Mask>(true);
		foreach (var mask in masks)
		{
			mask.graphic.material = null;
		}
	}

	public virtual void UpdateSelf(float width, int depth, bool expanded)
	{
		var cubeScale = m_Cube.transform.localScale;
		cubeScale.x = width;
		m_Cube.transform.localScale = cubeScale;

		if (depth > 0) // Lose one level of indentation because everything is a child of the header
			depth--;

		var indent = kIndent * depth;
		m_UIContainer.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, indent, width - indent);

		if (m_CuboidLayout)
			m_CuboidLayout.UpdateObjects();
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

	protected virtual void OnDropHoverStarted(BaseHandle handle)
	{
		setHighlight(handle.gameObject, true);
	}

	protected virtual void OnDropHoverEnded(BaseHandle handle)
	{
		setHighlight(handle.gameObject, false);
	}

	object GetDropObject(BaseHandle handle)
	{
		return GetDropObjectForFieldBlock(handle.transform.parent);
	}

	bool CanDrop(BaseHandle handle, object dropObject)
	{
		return CanDropForFieldBlock(handle.transform.parent, dropObject);
	}

	void ReceiveDrop(BaseHandle handle, object dropObject)
	{
		ReceiveDropForFieldBlock(handle.transform.parent, dropObject);
	}

	protected override void OnDragStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		base.OnDragStarted(baseHandle, eventData);
		m_DragObject = null;

		var fieldBlock = baseHandle.transform.parent;
		if (fieldBlock)
		{
			if (m_ClickCount == 0)
			{
				// Get RayInputField from direct children
				foreach (Transform child in fieldBlock.transform)
				{
					m_ClickedField = child.GetComponent<InputField>();
					if (m_ClickedField)
						break;
				}
				StartCoroutine(CheckSingleClick());
			}

			m_ClickCount++;
			m_SelectIsHeld = true;
			m_DragStarts[eventData.rayOrigin] = eventData.rayOrigin.position;

			// Detect double click
			var timeSinceLastClick = Time.realtimeSinceStartup - m_LastClickTime;
			m_LastClickTime = Time.realtimeSinceStartup;
			if (m_ClickCount > 1 && U.UI.IsDoubleClick(timeSinceLastClick))
			{
				CancelSingleClick();

				var clone = Instantiate(fieldBlock.gameObject, fieldBlock.parent) as GameObject;
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
				m_ClickedField = null; // Clear clicked field so we don't drag the value

				var graphics = clone.GetComponentsInChildren<Graphic>(true);
				foreach (var graphic in graphics)
					graphic.material = null;

				var renderers = clone.GetComponentsInChildren<Renderer>(true);
				foreach (var renderer in renderers)
					renderer.sharedMaterial = m_NoClipBackingCube;
			}
		}
	}

	protected override void OnDragging(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (m_ClickedField)
		{
			var rayOrigin = eventData.rayOrigin;
			m_DragDistance = (rayOrigin.position - m_DragStarts[rayOrigin]).magnitude;

			var numericField = m_ClickedField as NumericInputField;
			if (numericField)
			{
				if (m_DragDistance > NumericInputField.kDragDeadzone)
					CancelSingleClick();

				numericField.SliderDrag(eventData.rayOrigin);
			}
		}

		if (m_DragObject)
		{
			var previewOrigin = getPreviewOriginForRayOrigin(eventData.rayOrigin);
			U.Math.LerpTransform(m_DragObject, previewOrigin.position, kPreviewRotation, m_DragLerp);
		}
	}

	protected override void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		m_SelectIsHeld = false;

		var numericField = m_ClickedField as NumericInputField;
		if (numericField)
			numericField.EndDrag();

		var fieldBlock = baseHandle.transform.parent;
		if (fieldBlock)
		{
			if (m_DragObject)
				U.Object.Destroy(m_DragObject.gameObject);
		}

		base.OnDragEnded(baseHandle, eventData);
	}

	void CancelSingleClick()
	{
		m_ClickCount = 0;
	}

	IEnumerator CheckSingleClick()
	{
		var start = Time.realtimeSinceStartup;
		var currTime = 0f;
		while (m_SelectIsHeld || currTime < U.UI.kDoubleClickIntervalMax)
		{
			currTime = Time.realtimeSinceStartup - start;
			yield return null;
		}

		if (m_ClickCount == 1)
		{
			foreach (var inputField in m_InputFields)
				inputField.CloseKeyboard(m_ClickedField == null);

			if (m_ClickedField)
				m_ClickedField.OpenKeyboard();
		}

		m_ClickCount = 0;
	}

	protected virtual object GetDropObjectForFieldBlock(Transform fieldBlock)
	{
		return null;
	}

	protected virtual bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		return false;
	}

	protected virtual void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
	}

	public void ToggleExpanded()
	{
		toggleExpanded(data);
	}
}