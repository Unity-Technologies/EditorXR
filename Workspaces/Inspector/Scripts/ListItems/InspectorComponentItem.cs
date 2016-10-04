using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using Button = UnityEngine.VR.UI.Button;

public class InspectorComponentItem : InspectorListItem
{
	private const float kExpandArrowRotateSpeed = 0.4f;
	static readonly Quaternion kExpandedRotation = Quaternion.AngleAxis(90f, Vector3.forward);
	static readonly Quaternion kNormalRotation = Quaternion.identity;

	[SerializeField]
	private Button m_ExpandArrow;

	[SerializeField]
	private RawImage m_Icon;

	[SerializeField]
	private Toggle m_EnabledToggle;

	[SerializeField]
	private Text m_NameText;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var target = data.serializedObject.targetObject;
		var type = target.GetType();
		m_NameText.text = type.Name;

		StopAllCoroutines();
		StartCoroutine(GetAssetPreview());

		var enabled = EditorUtility.GetObjectEnabled(target);
		m_EnabledToggle.gameObject.SetActive(enabled != -1);
		m_EnabledToggle.isOn = enabled == 1;

		m_ExpandArrow.gameObject.SetActive(data.children != null);
	}

	IEnumerator GetAssetPreview()
	{
		m_Icon.texture = null;

		var target = data.serializedObject.targetObject;
		m_Icon.texture = AssetPreview.GetAssetPreview(target);

		while (AssetPreview.IsLoadingAssetPreview(target.GetInstanceID()))
		{
			m_Icon.texture = AssetPreview.GetAssetPreview(target);
			yield return null;
		}

		if (!m_Icon.texture)
			m_Icon.texture = AssetPreview.GetMiniThumbnail(target);
	}

	public override void UpdateSelf(float width, int depth)
	{
		base.UpdateSelf(width, depth);

		// Rotate arrow for expand state
		m_ExpandArrow.transform.localRotation = Quaternion.Lerp(m_ExpandArrow.transform.localRotation,
												data.expanded ? kExpandedRotation : kNormalRotation,
												kExpandArrowRotateSpeed);
	}

	public void ToggleExpanded()
	{
		data.expanded = !data.expanded;
	}

	public void SetEnabled(bool value)
	{
		var serializedObject = data.serializedObject;
		var target = serializedObject.targetObject;
		if (value != (EditorUtility.GetObjectEnabled(target) == 1))
		{
			EditorUtility.SetObjectEnabled(target, value);
			serializedObject.ApplyModifiedProperties();
		}
	}

	protected override void OnDragStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		// Components cannot be dragged and dropped (yet)
	}

	protected override object GetDropObject(Transform fieldBlock)
	{
		// Components cannot be dragged and dropped (yet)
		return null;
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