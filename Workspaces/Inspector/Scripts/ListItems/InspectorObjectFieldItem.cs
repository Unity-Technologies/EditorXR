using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;
using Object = UnityEngine.Object;

public class InspectorObjectFieldItem : InspectorPropertyItem
{
	[SerializeField]
	Text m_FieldLabel;

	[SerializeField]
	MeshRenderer m_Button;

	Type m_ObjectType;
	string m_ObjectTypeName;

#if UNITY_EDITOR
	public override void Setup(InspectorData data)
	{
		base.Setup(data);

		m_ObjectTypeName = U.Object.NicifySerializedPropertyType(m_SerializedProperty.type);
		m_ObjectType = U.Object.TypeNameToType(m_ObjectTypeName);

		SetObject(m_SerializedProperty.objectReferenceValue);
	}

	bool SetObject(Object obj)
	{
		var objectReference = m_SerializedProperty.objectReferenceValue;

		if (obj == null)
			m_FieldLabel.text = string.Format("None ({0})", m_ObjectTypeName);
		else
		{
			var objType = obj.GetType();
			if (!objType.IsAssignableFrom(m_ObjectType))
			{
				if (obj.Equals(objectReference)) // Show type mismatch for old serialized data
					m_FieldLabel.text = "Type Mismatch";
				return false;
			}
			m_FieldLabel.text = string.Format("{0} ({1})", obj.name, obj.GetType().Name);
		}

		if (obj == null && m_SerializedProperty.objectReferenceValue == null)
			return true;
		if (m_SerializedProperty.objectReferenceValue != null && m_SerializedProperty.objectReferenceValue.Equals(obj))
			return true;

		m_SerializedProperty.objectReferenceValue = obj;

		data.serializedObject.ApplyModifiedProperties();

		return true;
	}

	public void ClearButton()
	{
		SetObject(null);
	}

	protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
	{
		return m_SerializedProperty.objectReferenceValue;
	}

	protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		return dropObject is Object;
	}

	protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		SetObject(dropObject as Object);
	}

	public override void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material uiMaterial, Material textMaterial, Material noClipBackingCube, Material[] highlightMaterials)
	{
		base.SetMaterials(rowMaterial, backingCubeMaterial, uiMaterial, textMaterial, noClipBackingCube, highlightMaterials);
		m_Button.sharedMaterials = highlightMaterials;
	}
#endif
}