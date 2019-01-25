
using System;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorRectItem : InspectorPropertyItem
    {
        [SerializeField]
        NumericInputField[] m_CenterFields;

        [SerializeField]
        NumericInputField[] m_SizeFields;

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

            UpdateInputFields();
        }

        void UpdateInputFields()
        {
#if UNITY_EDITOR
            var rect = m_SerializedProperty.rectValue;

            for (var i = 0; i < m_CenterFields.Length; i++)
            {
                m_CenterFields[i].text = rect.center[i].ToString();
                m_CenterFields[i].ForceUpdateLabel();
                m_SizeFields[i].text = rect.size[i].ToString();
                m_SizeFields[i].ForceUpdateLabel();
            }
#endif
        }

        void UpdateInputFields(Rect rect)
        {
            for (var i = 0; i < m_CenterFields.Length; i++)
            {
                m_CenterFields[i].text = rect.center[i].ToString();
                m_CenterFields[i].ForceUpdateLabel();
                m_SizeFields[i].text = rect.size[i].ToString();
                m_SizeFields[i].ForceUpdateLabel();
            }
        }

        protected override void FirstTimeSetup()
        {
            base.FirstTimeSetup();

#if UNITY_EDITOR
            for (var i = 0; i < m_CenterFields.Length; i++)
            {
                var index = i;
                m_CenterFields[i].onValueChanged.AddListener(value =>
                {
                    if (SetValue(value, index, true))
                        data.serializedObject.ApplyModifiedProperties();
                });
                m_SizeFields[i].onValueChanged.AddListener(value =>
                {
                    if (SetValue(value, index))
                        data.serializedObject.ApplyModifiedProperties();
                });
            }
#endif
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();
            UpdateInputFields();
        }

        bool SetValue(string input, int index, bool center = false)
        {
            float value;
            if (!float.TryParse(input, out value))
                return false;

#if UNITY_EDITOR
            var rect = m_SerializedProperty.rectValue;
            var vector = center ? rect.center : rect.size;

            if (!Mathf.Approximately(vector[index], value))
            {
                vector[index] = value;
                if (center)
                    rect.center = vector;
                else
                    rect.size = vector;

                UpdateInputFields(rect);
                m_SerializedProperty.rectValue = rect;
                UpdateInputFields();
                return true;
            }
#endif

            return false;
        }

        protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
        {
            object dropObject = null;
            var inputFields = fieldBlock.GetComponentsInChildren<NumericInputField>();

#if UNITY_EDITOR
            if (inputFields.Length > 3) // If we've grabbed all of the fields
                dropObject = m_SerializedProperty.rectValue;

            if (inputFields.Length > 1) // If we've grabbed one vector
            {
                if (m_CenterFields.Intersect(inputFields).Any())
                    dropObject = m_SerializedProperty.rectValue.center;
                else
                    dropObject = m_SerializedProperty.rectValue.size;
            }
            else if (inputFields.Length > 0) // If we've grabbed a single field
                dropObject = inputFields[0].text;
#endif

            return dropObject;
        }

        protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            return dropObject is string || dropObject is Rect || dropObject is Vector2
                || dropObject is Vector3 || dropObject is Vector4;
        }

        protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            var str = dropObject as string;
            if (str != null)
            {
                var inputField = fieldBlock.GetComponentInChildren<NumericInputField>();
                var index = Array.IndexOf(m_SizeFields, inputField);
                if (index > -1 && SetValue(str, index))
                {
                    inputField.text = str;
                    inputField.ForceUpdateLabel();

                    FinalizeModifications();
                }

                index = Array.IndexOf(m_CenterFields, inputField);
                if (index > -1 && SetValue(str, index, true))
                {
                    inputField.text = str;
                    inputField.ForceUpdateLabel();

                    FinalizeModifications();
                }
            }

#if UNITY_EDITOR
            if (dropObject is Rect)
            {
                m_SerializedProperty.rectValue = (Rect)dropObject;

                UpdateInputFields();
                FinalizeModifications();
                data.serializedObject.ApplyModifiedProperties();
            }

            if (dropObject is Vector2 || dropObject is Vector3 || dropObject is Vector4)
            {
                var vector2 = (Vector2)dropObject;
                var inputField = fieldBlock.GetComponentInChildren<NumericInputField>();
                var rect = m_SerializedProperty.rectValue;

                if (m_CenterFields.Contains(inputField))
                    rect.center = vector2;
                else
                    rect.size = vector2;

                m_SerializedProperty.rectValue = rect;

                UpdateInputFields();
                FinalizeModifications();
                data.serializedObject.ApplyModifiedProperties();
            }
#endif
        }
    }
}

