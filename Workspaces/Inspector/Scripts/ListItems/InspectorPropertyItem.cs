using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Data;
using UnityEngine.UI;

abstract class InspectorPropertyItem : InspectorListItem, ITooltip, ITooltipPlacement, ISetTooltipVisibility
{
	[SerializeField]
	Text m_Label;

	public Transform tooltipTarget { get { return m_TooltipTarget; } }
	[SerializeField]
	Transform m_TooltipTarget;

	public Transform tooltipSource { get { return m_TooltipSource; } }
	[SerializeField]
	Transform m_TooltipSource;

	public TextAlignment tooltipAlignment { get { return TextAlignment.Right; } }

	public Action<ITooltip> showTooltip { get; set; }
	public Action<ITooltip> hideTooltip { get; set; }

#if UNITY_EDITOR
	public string tooltipText { get { return m_SerializedProperty.tooltip; } }

	protected SerializedProperty m_SerializedProperty;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_SerializedProperty = ((PropertyData)data).property;

		m_Label.text = m_SerializedProperty.displayName;
	}

	public override void OnObjectModified()
	{
		base.OnObjectModified();

		m_SerializedProperty = data.serializedObject.FindProperty(m_SerializedProperty.propertyPath);
	}

	protected void FinalizeModifications()
	{
		Undo.IncrementCurrentGroup();
		data.serializedObject.ApplyModifiedProperties();
	}
#else
	public string tooltipText { get { return string.Empty; } }
#endif
}