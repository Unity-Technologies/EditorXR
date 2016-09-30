using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public abstract class InspectorListItem : DraggableListItem<InspectorData>, IHighlight, IDroppable, IDropReciever
{
	private const float kIndent = 0.02f;
	private const float kMinDragDistance = 0.01f;

	private static readonly Quaternion previewRotation = Quaternion.AngleAxis(90, Vector3.right);

	protected CuboidLayout m_CuboidLayout;

	[SerializeField]
	private BaseHandle m_Cube;

	[SerializeField]
	private RectTransform m_UIContainer;

	private ClipText[] m_ClipTexts;

	private Material m_NoClipBackingCube;

	private bool m_Setup;

	private readonly Dictionary<Transform, Vector3> m_DragStarts = new Dictionary<Transform, Vector3>();

	private float m_LastClickTime;
	private int m_ClickCount;
	private bool m_SelectIsHeld;
	private float m_DragDistance;
	private RayInputField m_ClickedField;
	private Vector3 m_PointerPosition;

	public bool setup { get; set; }

	public Action<GameObject, bool> setHighlight { set; private get; }

	public GetDropRecieverDelegate getCurrentDropReciever { protected get; set; }
	public Func<Transform, object> getCurrentDropObject { protected get; set; }

	public Action<Transform, IDropReciever, GameObject> setCurrentDropReciever { private get; set; }
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
		var rayOrigin = eventData.rayOrigin;
		var dropObject = getCurrentDropObject(rayOrigin);

		// TODO: red hover state when TestDrop fails
		if (dropObject == null || TestDrop(handle.gameObject, dropObject))
		{
			setHighlight(handle.gameObject, true);
			setCurrentDropReciever(rayOrigin, this, handle.gameObject);
		}
	}

	protected virtual void OnHoverEnded(BaseHandle handle, HandleEventData eventData)
	{
		var rayOrigin = eventData.rayOrigin;
		var dropObject = getCurrentDropObject(rayOrigin);

		if (dropObject == null || TestDrop(handle.gameObject, dropObject))
		{
			setHighlight(handle.gameObject, false);
			setCurrentDropReciever(eventData.rayOrigin, null, handle.gameObject);
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
					m_ClickedField = child.GetComponent<RayInputField>();
					if (m_ClickedField)
					{
						StartCoroutine(CheckSingleClick());
						break;
					}
				}
			}
			m_PointerPosition = eventData.rayOrigin.position;
			m_ClickCount++;
			m_SelectIsHeld = true;
			m_DragStarts[eventData.rayOrigin] = fieldBlock.position;

			// Detect double click
			var timeSinceLastClick = Time.realtimeSinceStartup - m_LastClickTime;
			m_LastClickTime = Time.realtimeSinceStartup;
			if (m_ClickCount > 1 && U.UI.DoubleClick(timeSinceLastClick))
			{
				m_ClickCount = 0;

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
			var currentPosition = m_ClickedField.transform.parent.position;
			m_DragDistance = (currentPosition - m_DragStarts[eventData.rayOrigin]).magnitude;
			//TODO: drag sliding
		}

		if (m_DragObject)
			positionPreview(m_DragObject, getPreviewOriginForRayOrigin(eventData.rayOrigin), m_DragLerp, previewRotation);
	}

	protected override void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		m_SelectIsHeld = false;
		var fieldBlock = baseHandle.transform.parent;
		if (fieldBlock)
		{
			if (m_DragObject)
			{
				var rayOrigin = eventData.rayOrigin;
				GameObject target;
				var dropReciever = getCurrentDropReciever(rayOrigin, out target);
				var dropObject = getCurrentDropObject(rayOrigin);
				if (dropReciever != null)
					dropReciever.RecieveDrop(target, dropObject);
				setCurrentDropObject(rayOrigin, null);

				U.Object.Destroy(m_DragObject.gameObject);
			}
		}

		base.OnDragEnded(baseHandle, eventData);
	}

	private IEnumerator CheckSingleClick()
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
			if (m_ClickedField && m_DragDistance < kMinDragDistance)
				m_ClickedField.ToggleKeyboard(m_PointerPosition);
		}

		m_ClickCount = 0;
	}

	protected abstract object GetDropObject(Transform fieldBlock);

	public abstract bool TestDrop(GameObject target, object droppedObject);

	public abstract bool RecieveDrop(GameObject target, object droppedObject);
}