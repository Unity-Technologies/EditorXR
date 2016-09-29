using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class InspectorUnimplementedItem : InspectorPropertyItem
{
	[SerializeField]
	private Text m_TypeLabel;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_TypeLabel.text = U.Object.NiceSerializedPropertyType(m_SerializedProperty.type);
	}

	protected override void DropItem(Transform fieldBlock, IDropReciever dropReciever, GameObject target)
	{
		// Unimplemented
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