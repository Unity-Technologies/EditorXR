using System;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Data;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;
using UnityEngine.UI;
using InputField = UnityEngine.Experimental.EditorVR.UI.InputField;

public abstract class InspectorListItem : DraggableListItem<InspectorData>, ISetHighlight, IRequestStencilRef
{
	const float kIndent = 0.02f;

	protected CuboidLayout m_CuboidLayout;

	protected InputField[] m_InputFields;

	protected override bool singleClickDrag { get { return false; } }

	protected override BaseHandle clickedHandle
	{
		get { return m_ClickedHandle; }
		set
		{
			m_ClickedHandle = value;
			m_ClickedField = null;

			if (m_ClickedHandle != null)
			{
				var fieldBlock = m_ClickedHandle.transform.parent;
				if (fieldBlock)
				{
					// Get RayInputField from direct children
					foreach (Transform child in fieldBlock.transform)
					{
						var clickedField = child.GetComponent<InputField>();
						if (clickedField)
						{
							m_ClickedField = clickedField;
							break;
						}
					}
				}
			}
		}
	}
	BaseHandle m_ClickedHandle;
	protected InputField m_ClickedField;

	[SerializeField]
	BaseHandle m_Cube;

	[SerializeField]
	RectTransform m_UIContainer;

	ClipText[] m_ClipTexts;

	Material m_NoClipBackingCube;
	Material[] m_NoClipHighlightMaterials;

	bool m_Setup;

	public bool setup { get; set; }

	public Action<GameObject, bool> setHighlight { private get; set; }

	public Action<InspectorData> toggleExpanded { private get; set; }

	public Func<byte> requestStencilRef { private get; set; }

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

	public virtual void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material uiMaterial, Material textMaterial, Material noClipBackingCube, Material[] highlightMaterials, Material[] noClipHighlightMaterials)
	{
		m_NoClipBackingCube = noClipBackingCube;
		m_NoClipHighlightMaterials = noClipHighlightMaterials;

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

	protected override void OnDoubleClick(BaseHandle baseHandle, HandleEventData eventData)
	{
		var fieldBlock = baseHandle.transform.parent;
		if (fieldBlock)
		{
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
			m_ClickedField = null; // Prevent dragging on NumericFields

			StartCoroutine(Magnetize());

			var graphics = clone.GetComponentsInChildren<Graphic>(true);
			foreach (var graphic in graphics)
			{
				graphic.material = null;
			}

			var stencilRef = requestStencilRef();
			var renderers = clone.GetComponentsInChildren<Renderer>(true);
			foreach (var renderer in renderers)
			{
				if (renderer.sharedMaterials.Length > 1)
				{
					foreach (var material in m_NoClipHighlightMaterials)
					{
						material.SetInt("_StencilRef", stencilRef);
					}
					renderer.sharedMaterials = m_NoClipHighlightMaterials;
				}
				else
				{
					renderer.sharedMaterial = m_NoClipBackingCube;
					m_NoClipBackingCube.SetInt("_StencilRef", stencilRef);
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
			U.Math.LerpTransform(m_DragObject, previewOrigin.position, 
				U.Math.ConstrainYawRotation(U.Camera.GetMainCamera().transform.rotation), m_DragLerp);
		}
	}

	protected override void OnSingleClickDrag(BaseHandle handle, HandleEventData eventData, Vector3 dragStart)
	{
		if (m_ClickedField)
		{
			var numericField = m_ClickedField as NumericInputField;
			if (numericField)
			{
				numericField.SliderDrag(eventData.rayOrigin);
			}
		}
	}

	protected override void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		var numericField = m_ClickedField as NumericInputField;
		if (numericField)
			numericField.EndDrag();


		if (m_DragObject)
			U.Object.Destroy(m_DragObject.gameObject);

		base.OnDragEnded(baseHandle, eventData);
	}

	protected override void OnSingleClick(BaseHandle handle, HandleEventData eventData)
	{
		if (m_ClickedField)
		{
			foreach (var inputField in m_InputFields)
			{
				inputField.CloseKeyboard(m_ClickedField == null);
			}

			m_ClickedField.OpenKeyboard();
		}
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
}