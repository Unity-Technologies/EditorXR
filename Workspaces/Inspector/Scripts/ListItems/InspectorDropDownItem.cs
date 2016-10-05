using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.VR.UI;

public class InspectorDropDownItem : InspectorPropertyItem
{
	const string kNothing = "Nothing";
	const string kEverything = "Everything";

	[SerializeField]
	DropDown m_DropDown;

	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		if (m_SerializedProperty.propertyType == SerializedPropertyType.LayerMask)
		{
			m_DropDown.multiSelect = true;
			var options = new List<string>();
			options.Add(kNothing);
			options.Add(kEverything);
			options.AddRange(InternalEditorUtility.layers);
			m_DropDown.options = options.ToArray();

			if (m_SerializedProperty.intValue == 0)
				m_DropDown.values = new[] { 0 };
			else if (m_SerializedProperty.intValue == ~0) // Everything
			{
				m_DropDown.values = EverythingValues();
				m_DropDown.LabelOverride("Everything");
			}
			else
				m_DropDown.values = LayerMaskToIndices(m_SerializedProperty.intValue);
			
		}
		else {
			m_DropDown.multiSelect = false;
			m_DropDown.options = m_SerializedProperty.enumDisplayNames;
			m_DropDown.value = m_SerializedProperty.enumValueIndex;
		}
	}

	protected override void FirstTimeSetup()
	{
		base.FirstTimeSetup();
		m_DropDown.onValueChanged += OnValueChanged;
	}

	void OnValueChanged(int clicked, int[] values)
	{
		if (m_SerializedProperty.propertyType == SerializedPropertyType.LayerMask)
		{
			if (clicked == 0) // Clicked "Nothing"
			{
				values = new[] { 0 };
				m_DropDown.values = values;

				if (m_SerializedProperty.intValue == 0)
					return;
				m_SerializedProperty.intValue = 0;
				data.serializedObject.ApplyModifiedProperties();
			}
			else if (clicked == 1)  // Clicked "Everything"
			{
				m_DropDown.values = EverythingValues();
				m_DropDown.LabelOverride("Everything");

				if (m_SerializedProperty.intValue == ~0)
					return;
				m_SerializedProperty.intValue = ~0;
				data.serializedObject.ApplyModifiedProperties();
			}
			else
			{
				// Remove "Everything" and "Nothing"
				var list = new List<int>(values);
				if (list.Remove(0) || list.Remove(1))
				{
					values = list.ToArray();
					m_DropDown.values = values;
				}

				var layerMask = IndicesToLayerMask(values);
				if (m_SerializedProperty.intValue != layerMask)
				{
					m_SerializedProperty.intValue = layerMask;
					data.serializedObject.ApplyModifiedProperties();
				}
			}
		}
		else
		{
			if (m_SerializedProperty.enumValueIndex != values[0])
			{
				m_SerializedProperty.enumValueIndex = values[0];
				data.serializedObject.ApplyModifiedProperties();
			}
		}
	}

	int[] EverythingValues()
	{
		var values = new int[InternalEditorUtility.layers.Length + 1];
		for (int i = 0; i < values.Length; i++)
			values[i] = i + 1;
		return values;
	}

	protected override object GetDropObject(Transform fieldBlock)
	{
		return m_DropDown.multiSelect ? (object) m_DropDown.values : m_DropDown.value;
	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		return m_DropDown.multiSelect && droppedObject is int[]
			|| !m_DropDown.multiSelect && droppedObject is int;
	}

	public override bool RecieveDrop(GameObject target, object droppedObject)
	{
		if (m_DropDown.multiSelect && droppedObject is int[])
		{
			m_DropDown.values = (int[]) droppedObject;
			return true;
		}

		if (!m_DropDown.multiSelect && droppedObject is int)
		{
			m_DropDown.value = (int) droppedObject;
			return true;
		}
		return false;
	}

	static int[] LayerMaskToIndices(int layerMask)
	{
		int mask = 1;
		List<int> layers = new List<int>();
		for (int i = 0; i < 32; i++)
		{
			if ((layerMask & mask) != 0)
				layers.Add(Array.IndexOf(InternalEditorUtility.layers, LayerMask.LayerToName(i)) + 2);
			mask <<= 1;
		}
		return layers.ToArray();
	}

	static int IndicesToLayerMask(int[] indices)
	{
		var layerMask = 0;
		foreach (var index in indices)
		{
			if (index == 0) // Nothing
				return 0;
			if (index == 1) // Everything
				return ~0;
			var realIndex = index - 2; // Account for "Nothing" and "Everything"
			if (realIndex >= 0)
				layerMask |= 1 << LayerMask.NameToLayer(InternalEditorUtility.layers[realIndex]);
		}

		return layerMask;
	}
}