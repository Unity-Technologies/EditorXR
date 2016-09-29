using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;

public class InspectorComponentItem : InspectorListItem
{
	private const float kExpandArrowRotateSpeed = 0.4f;

	[SerializeField]
	private Button m_ExpandArrow;

	[SerializeField]
	private RawImage m_Icon;

	[SerializeField]
	private Toggle m_EnabledToggle;

	[SerializeField]
	private Text m_NameText;

	[SerializeField]
	private Button m_GearMenu;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var target = data.serializedObject.targetObject;
		var type = target.GetType();
		m_NameText.text = type.Name;
		m_Icon.texture = AssetPreview.GetMiniTypeThumbnail(type);

		m_EnabledToggle.gameObject.SetActive(EditorUtility.GetObjectEnabled(target) != -1);
		m_EnabledToggle.isOn = EditorUtility.GetObjectEnabled(target) == 1;

		m_ExpandArrow.gameObject.SetActive(data.children != null);
	}

	public void GetMaterials(out Material textMaterial, out Material expandArrowMaterial, out Material gearMaterial)
	{
		textMaterial = Instantiate(m_NameText.material);
		expandArrowMaterial = Instantiate(m_ExpandArrow.GetComponent<Renderer>().sharedMaterial);
		gearMaterial = Instantiate(m_GearMenu.GetComponent<Renderer>().sharedMaterial);
	}

	public void SwapMaterials(Material textMaterial, Material expandArrowMaterial, Material gearMaterial)
	{
		m_NameText.material = textMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
		m_GearMenu.GetComponent<Renderer>().sharedMaterial = gearMaterial;
	}

	public override void UpdateSelf(float width, int depth)
	{
		base.UpdateSelf(width, depth);

		// Rotate arrow for expand state
		m_ExpandArrow.transform.localRotation = Quaternion.Lerp(m_ExpandArrow.transform.localRotation,
												data.expanded ? Quaternion.AngleAxis(90f, Vector3.back) : Quaternion.identity,
												kExpandArrowRotateSpeed);
	}

	public void ToggleExpanded()
	{
		data.expanded = !data.expanded;
	}

	public void SetEnabled(bool value)
	{
		EditorUtility.SetObjectEnabled(data.serializedObject.targetObject, value);
	}

	protected override void OnDragStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		// Components cannot be dragged and dropped (yet)
	}

	protected override void DropItem(Transform fieldBlock, IDropReciever dropReciever, GameObject target)
	{
		// Components cannot be dragged and dropped (yet)
	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		return false;
	}

	public override bool RecieveDrop(GameObject target, object droppedObject)
	{
		return false;
	}
}