using UnityEngine;
using UnityEngine.UI;

public class InspectorHeaderItem : InspectorListItem
{
	[SerializeField]
	private Toggle m_ActiveToggle;

	[SerializeField]
	private InputField m_NameField;

	[SerializeField]
	private Toggle m_StaticToggle;
}