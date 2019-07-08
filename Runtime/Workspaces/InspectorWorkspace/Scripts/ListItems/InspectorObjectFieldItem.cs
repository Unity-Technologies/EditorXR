using System;
using TMPro;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
#if UNITY_EDITOR
    sealed class InspectorObjectFieldItem : InspectorPropertyItem
    {
#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_FieldLabel;

        [SerializeField]
        MeshRenderer m_Button;
#pragma warning restore 649

        Type m_ObjectType;
        string m_ObjectTypeName;

        public override void Setup(InspectorData datum, bool firstTime = false)
        {
            base.Setup(datum, firstTime);

#if UNITY_EDITOR
            m_ObjectTypeName = EditorUtils.NicifySerializedPropertyType(m_SerializedProperty.type);
            m_ObjectType = EditorUtils.TypeNameToType(m_ObjectTypeName);
#endif

            OnObjectModified();
        }

        bool SetObject(Object obj)
        {
            if (!IsAssignable(obj))
                return false;

#if UNITY_EDITOR
            if (obj == null && m_SerializedProperty.objectReferenceValue == null)
                return true;

            if (m_SerializedProperty.objectReferenceValue != null && m_SerializedProperty.objectReferenceValue.Equals(obj))
                return true;

            m_SerializedProperty.objectReferenceValue = obj;
#endif

            FinalizeModifications();

            OnObjectModified();

            return true;
        }

        public void ClearButton()
        {
            SetObject(null);
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();
            UpdateUI();
        }

        public void UpdateUI()
        {
#if UNITY_EDITOR
            var obj = m_SerializedProperty.objectReferenceValue;
            if (obj == null)
            {
                m_FieldLabel.text = string.Format("None ({0})", m_ObjectTypeName);
                return;
            }
            if (!IsAssignable(obj))
            {
                m_FieldLabel.text = "Type Mismatch";
                return;
            }
            m_FieldLabel.text = string.Format("{0} ({1})", obj.name, obj.GetType().Name);
#endif
        }

        protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
        {
#if UNITY_EDITOR
            return m_SerializedProperty.objectReferenceValue;
#else
            return null;
#endif
        }

        protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            var obj = dropObject as Object;
            return obj != null && IsAssignable(obj);
        }

        protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            SetObject(dropObject as Object);
        }

        public override void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material uiMaterial, Material uiMaskMaterial, Material noClipBackingCube, Material[] highlightMaterials, Material[] noClipHighlightMaterials)
        {
            base.SetMaterials(rowMaterial, backingCubeMaterial, uiMaterial, uiMaskMaterial, noClipBackingCube, highlightMaterials, noClipHighlightMaterials);
            m_Button.sharedMaterials = highlightMaterials;
        }

        bool IsAssignable(Object obj)
        {
            return obj == null || obj.GetType().IsAssignableFrom(m_ObjectType);
        }
    }
#else
    sealed class InspectorObjectFieldItem : InspectorPropertyItem
    {
        [SerializeField]
        TextMeshProUGUI m_FieldLabel;

        [SerializeField]
        MeshRenderer m_Button;
    }
#endif
}
