using System;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorColorItem : InspectorPropertyItem
    {
        public override void Setup(InspectorData data)
        {
            base.Setup(data);

            UpdateInputFields();
        }

        protected override void FirstTimeSetup()
        {
            base.FirstTimeSetup();

#if UNITY_EDITOR
            for (var i = 0; i < m_InputFields.Length; i++)
            {
                var index = i;
                m_InputFields[i].onValueChanged.AddListener(value =>
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

        public bool SetValue(string input, int index)
        {
#if UNITY_EDITOR
            float value;
            if (!float.TryParse(input, out value))
                return false;

            var color = m_SerializedProperty.colorValue;
            if (!Mathf.Approximately(color[index], value))
            {
                color[index] = value;
                m_SerializedProperty.colorValue = color;

                UpdateInputFields();

                return true;
            }
#endif

            return false;
        }

        void UpdateInputFields()
        {
#if UNITY_EDITOR
            var color = m_SerializedProperty.colorValue;

            for (var i = 0; i < 4; i++)
            {
                m_InputFields[i].text = color[i].ToString();
                m_InputFields[i].ForceUpdateLabel();
            }
#endif
        }

        protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
        {
            object dropObject = null;

#if UNITY_EDITOR
            var inputfields = fieldBlock.GetComponentsInChildren<NumericInputField>();
            if (inputfields.Length > 1)
            {
                dropObject = m_SerializedProperty.colorValue;
            }
            else if (inputfields.Length > 0)
                dropObject = inputfields[0].text;
#endif

            return dropObject;
        }

        protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            return dropObject is string || dropObject is Vector2 || dropObject is Vector3
                || dropObject is Vector4 || dropObject is Quaternion || dropObject is Color;
        }

        protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            var str = dropObject as string;
            if (str != null)
            {
                var inputField = fieldBlock.GetComponentInChildren<NumericInputField>();
                var index = Array.IndexOf(m_InputFields, inputField);

                if (SetValue(str, index))
                {
                    inputField.text = str;
                    inputField.ForceUpdateLabel();

                    FinalizeModifications();
                }
            }

#if UNITY_EDITOR
            if (dropObject is Color)
            {
                m_SerializedProperty.colorValue = (Color)dropObject;

                UpdateInputFields();

                FinalizeModifications();
            }

            var color = m_SerializedProperty.colorValue;
            if (dropObject is Vector2)
            {
                var vector2 = (Vector2)dropObject;
                color.r = vector2.x;
                color.g = vector2.y;
                m_SerializedProperty.colorValue = color;

                UpdateInputFields();

                FinalizeModifications();
            }

            if (dropObject is Vector3)
            {
                var vector3 = (Vector3)dropObject;
                color.r = vector3.x;
                color.g = vector3.y;
                color.b = vector3.z;
                m_SerializedProperty.colorValue = color;

                UpdateInputFields();

                FinalizeModifications();
            }

            if (dropObject is Vector4)
            {
                var vector4 = (Vector4)dropObject;
                color.r = vector4.x;
                color.g = vector4.y;
                color.b = vector4.z;
                color.a = vector4.w;
                m_SerializedProperty.colorValue = color;

                UpdateInputFields();

                FinalizeModifications();
            }
#endif
        }
    }
}
