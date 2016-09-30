using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InspectorHeaderItem : InspectorListItem
{
	[SerializeField]
	private RawImage m_Icon;

	[SerializeField]
	private Toggle m_ActiveToggle;

	[SerializeField]
	private StandardInputField m_NameField;

	[SerializeField]
	private Toggle m_StaticToggle;

	// TODO: Add dropdown for different static types
	
	private GameObject m_TargetGameObject;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		var target = data.serializedObject.targetObject;

		m_Icon.texture = AssetPreview.GetMiniThumbnail(target);
		m_TargetGameObject = target as GameObject;

		if (m_TargetGameObject)
		{
			m_ActiveToggle.isOn = m_TargetGameObject.activeSelf;
			m_StaticToggle.isOn = m_TargetGameObject.isStatic;
		}

		m_NameField.text = target.name;
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

	protected override object GetDropObject(Transform fieldBlock)
	{
		var inputField = fieldBlock.GetComponentInChildren<StandardInputField>();
		if (inputField)
			return inputField.text;
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