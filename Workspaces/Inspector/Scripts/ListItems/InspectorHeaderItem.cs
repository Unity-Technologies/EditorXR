using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.UI;
using InputField = UnityEngine.VR.UI.InputField;

public class InspectorHeaderItem : InspectorListItem
{
	[SerializeField]
	RawImage m_Icon;

	[SerializeField]
	Toggle m_ActiveToggle;

	[SerializeField]
	StandardInputField m_NameField;

	[SerializeField]
	Toggle m_StaticToggle;
	// TODO: Add dropdown for different static types

	public Toggle lockToggle { get { return m_LockToggle; } }
	[SerializeField]
	Toggle m_LockToggle;

	[SerializeField]
	DropDown m_TagDropDown;

	[SerializeField]
	DropDown m_LayerDropDown;

	GameObject m_TargetGameObject;
	
	public Action<bool> setLocked { private get; set; }

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var target = data.serializedObject.targetObject;

		StopAllCoroutines();
		StartCoroutine(GetAssetPreview());

		m_TargetGameObject = target as GameObject;

		if (m_TargetGameObject)
		{
			m_ActiveToggle.isOn = m_TargetGameObject.activeSelf;
			m_StaticToggle.isOn = m_TargetGameObject.isStatic;
		}

		m_NameField.text = target.name;
		m_NameField.ForceUpdateLabel();

		if (m_TargetGameObject)
		{
			var tags = UnityEditorInternal.InternalEditorUtility.tags;
			m_TagDropDown.options = tags;
			var tagIndex = Array.IndexOf(tags, m_TargetGameObject.tag);
			if (tagIndex > -1)
				m_TagDropDown.value = tagIndex;
			m_TagDropDown.onValueChanged += SetTag;

			var layers = UnityEditorInternal.InternalEditorUtility.layers;
			m_LayerDropDown.options = layers;
			var layerIndex = Array.IndexOf(layers, LayerMask.LayerToName(m_TargetGameObject.layer));
			if (layerIndex > -1)
				m_LayerDropDown.value = layerIndex;
			m_LayerDropDown.onValueChanged += SetLayer;
		}
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

	public void SetActive(bool active)
	{
		// TODO: Add choice dialog for whether to set in children
		if (m_TargetGameObject != null && m_TargetGameObject.activeSelf != active)
			m_TargetGameObject.SetActive(active);
	}

	public void SetName(string name)
	{
		var target = data.serializedObject.targetObject;
		if (!target.name.Equals(name))
			target.name = name;
	}

	public void SetStatic(bool isStatic)
	{
		// TODO: Add choice dialog for whether to set in children
		if(m_TargetGameObject != null && m_TargetGameObject.isStatic != isStatic)
			m_TargetGameObject.isStatic = isStatic;
	}

	public void SetLock(bool isLocked)
	{
		if (setLocked != null)
			setLocked(isLocked);
	}

	void SetTag(int val, int[] values)
	{
		var tags = UnityEditorInternal.InternalEditorUtility.tags;
		var tag = tags[values[0]];
		if(!m_TargetGameObject.tag.Equals(tag))
			m_TargetGameObject.tag = tag;
	}

	void SetLayer(int val, int[] values)
	{
		var layers = UnityEditorInternal.InternalEditorUtility.layers;
		var layer = LayerMask.NameToLayer(layers[values[0]]);
		if (m_TargetGameObject.layer != layer)
			m_TargetGameObject.layer = layer;
	}

	protected override object GetDropObject(Transform fieldBlock)
	{
		var inputField = fieldBlock.GetComponentInChildren<StandardInputField>();
		if (inputField)
			return inputField.text;
		return null;
	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		var inputFields = target.transform.parent.GetComponentsInChildren<InputField>();
		return droppedObject is string && inputFields.Contains(m_NameField);
	}

	public override bool RecieveDrop(GameObject target, object droppedObject)
	{
		if (!TestDrop(target, droppedObject))
			return false;
		m_NameField.text = (string)droppedObject;
		m_NameField.ForceUpdateLabel();
		return false;
	}
}