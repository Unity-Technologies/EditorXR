using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Tools;
using UnityEngine.VR.UI;
using UnityEngine.VR.Utilities;
using InputField = UnityEngine.VR.UI.InputField;

public abstract class InspectorListItem : DraggableListItem<InspectorData>, IHighlight, IDroppable, IDropReceiver
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

	public GetDropReceiverDelegate getCurrentDropReceiver { protected get; set; }
	public Func<Transform, object> getCurrentDropObject { protected get; set; }

	public Action<Transform, IDropReceiver, GameObject> setCurrentDropReceiver { private get; set; }
	public Action<Transform, object> setCurrentDropObject { private get; set; }

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

			// Toggles can't be dragged
			if (handle.transform.parent.GetComponentInChildren<Toggle>())
				continue;

			handle.dragStarted += OnDragStarted;
			handle.dragging += OnDragging;
			handle.dragEnded += OnDragEnded;

			handle.hoverStarted += OnHoverStarted;
			handle.hoverEnded += OnHoverEnded;
		}

		m_InputFields = GetComponentsInChildren<InputField>(true);
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

		if (depth > 0) // Lose one level of indentation because everything is a child of the header
			depth--;

		var indent = kIndent * depth;
		m_UIContainer.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, indent, width - indent);

		if(m_CuboidLayout)
			m_CuboidLayout.UpdateCubes();
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
		var rayOrigin = eventData.rayOrigin;
		var dropObject = getCurrentDropObject(rayOrigin);

		// TODO: red hover state when CanDrop fails
		if (dropObject == null || CanDrop(handle.gameObject, dropObject))
		{
			setHighlight(handle.gameObject, true);
			setCurrentDropReceiver(rayOrigin, this, handle.gameObject);
		}
	}

	protected virtual void OnHoverEnded(BaseHandle handle, HandleEventData eventData)
	{
		var rayOrigin = eventData.rayOrigin;
		if (rayOrigin == null) // BaseHandle.OnDisable sends a null rayOrigin
			return;

		var dropObject = getCurrentDropObject(rayOrigin);

		if (dropObject == null || CanDrop(handle.gameObject, dropObject))
		{
			setHighlight(handle.gameObject, false);
			setCurrentDropReceiver(eventData.rayOrigin, null, handle.gameObject);
		}
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
					{
						StartCoroutine(CheckSingleClick());
						break;
					}
				}
			}

			m_ClickCount++;
			m_SelectIsHeld = true;
			m_DragStarts[eventData.rayOrigin] = eventData.rayOrigin.position;

			// Detect double click
			var timeSinceLastClick = Time.realtimeSinceStartup - m_LastClickTime;
			m_LastClickTime = Time.realtimeSinceStartup;
			if (m_ClickCount > 1 && U.UI.DoubleClick(timeSinceLastClick))
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
				setCurrentDropObject(eventData.rayOrigin, GetDropObject(fieldBlock));
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
				if(m_DragDistance > NumericInputField.kDragDeadzone)
					CancelSingleClick();

				numericField.SliderDrag(eventData.rayOrigin);
			}
		}

		if (m_DragObject)
			preview(m_DragObject, getPreviewOriginForRayOrigin(eventData.rayOrigin), m_DragLerp, kPreviewRotation);
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
			{
				var rayOrigin = eventData.rayOrigin;
				GameObject target;
				var dropReceiver = getCurrentDropReceiver(rayOrigin, out target);
				var dropObject = getCurrentDropObject(rayOrigin);
				if (dropReceiver != null)
					dropReceiver.ReceiveDrop(target, dropObject);
				setCurrentDropObject(rayOrigin, null);

				U.Object.Destroy(m_DragObject.gameObject);
			}
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
				inputField.Close();

			if (m_ClickedField)
				m_ClickedField.Open();
		}

		m_ClickCount = 0;
	}

	protected abstract object GetDropObject(Transform fieldBlock);

	public abstract bool CanDrop(GameObject target, object droppedObject);

	public abstract bool ReceiveDrop(GameObject target, object droppedObject);
}