using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Data;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.UI;
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

		UpdateVisuals(m_SerializedProperty.objectReferenceValue);
	}

	bool SetObject(Object obj)
	{
		if (!TestAssignability(obj))
			return false;

		if (obj == null && m_SerializedProperty.objectReferenceValue == null)
			return true;

		if (m_SerializedProperty.objectReferenceValue != null && m_SerializedProperty.objectReferenceValue.Equals(obj))
			return true;

		UpdateVisuals(obj);

		blockUndoPostProcess();
		Undo.RecordObject(data.serializedObject.targetObject, "EditorVR Inspector");

		m_SerializedProperty.objectReferenceValue = obj;

		data.serializedObject.ApplyModifiedProperties();

		return true;
	}

	public void ClearButton()
	{
		SetObject(null);
	}

	bool TestAssignability(Object obj)
	{
		return obj == null || obj.GetType().IsAssignableFrom(m_ObjectType);
	}

	void UpdateVisuals(Object obj)
	{
		if (obj == null)
		{
			m_FieldLabel.text = string.Format("None ({0})", m_ObjectTypeName);
			return;
		}

		if (!TestAssignability(obj))
		{
			m_FieldLabel.text = "Type Mismatch";
			return;
		}

		m_FieldLabel.text = string.Format("{0} ({1})", obj.name, obj.GetType().Name);
	}

	protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
	{
		return m_SerializedProperty.objectReferenceValue;
	}

	protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		var obj = dropObject as Object;
		return obj != null && TestAssignability(obj);
	}

	protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
	{
		SetObject(dropObject as Object);
	}

	public override void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material uiMaterial, Material textMaterial, Material noClipBackingCube, Material[] highlightMaterials, Material[] noClipHighlightMaterials)
	{
		base.SetMaterials(rowMaterial, backingCubeMaterial, uiMaterial, textMaterial, noClipBackingCube, highlightMaterials, noClipHighlightMaterials);
		m_Button.sharedMaterials = highlightMaterials;
	}
#endif
}