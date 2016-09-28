using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class InspectorPropertyItem : InspectorListItem
{
	[SerializeField]
	private Text m_Label;

	protected SerializedProperty m_SerializedProperty;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_SerializedProperty = ((PropertyData)data).property;

		m_Label.text = m_SerializedProperty.displayName;
	}

	protected override void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		var dropReciever = getCurrentDropReciever(eventData.rayOrigin);
		if (dropReciever != null)
			dropReciever.OnDrop(m_SerializedProperty);

		U.Object.Destroy(m_DragObject.gameObject);
		base.OnDragEnded(baseHandle, eventData);
	}
}