using System;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorBoundsItem : InspectorPropertyItem
    {
#pragma warning disable 649
        [SerializeField]
        NumericInputField[] m_CenterFields;

        [SerializeField]
        NumericInputField[] m_ExtentsFields;
#pragma warning restore 649

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

            UpdateInputFields();
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
                m_ExtentsFields[i].onValueChanged.AddListener(value =>
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

        void UpdateInputFields()
        {
#if UNITY_EDITOR
            var bounds = m_SerializedProperty.boundsValue;

            for (var i = 0; i < m_CenterFields.Length; i++)
            {
                m_CenterFields[i].text = bounds.center[i].ToString();
                m_ExtentsFields[i].text = bounds.extents[i].ToString();
            }
#endif
        }

        bool SetValue(string input, int index, bool center = false)
        {
#if UNITY_EDITOR
            float value;
            if (!float.TryParse(input, out value))
                return false;

            var bounds = m_SerializedProperty.boundsValue;
            var vector = center ? bounds.center : bounds.extents;

            if (!Mathf.Approximately(vector[index], value))
            {
                vector[index] = value;
                if (center)
                    bounds.center = vector;
                else
                    bounds.extents = vector;

                m_SerializedProperty.boundsValue = bounds;

                UpdateInputFields();

                return true;
            }
#endif

            return false;
        }

        protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
        {
            object dropObject = null;
#if UNITY_EDITOR
            var inputFields = fieldBlock.GetComponentsInChildren<NumericInputField>();

            if (inputFields.Length > 3) // If we've grabbed all of the fields
                dropObject = m_SerializedProperty.boundsValue;
            if (inputFields.Length > 1) // If we've grabbed one vector
            {
                if (m_CenterFields.Intersect(inputFields).Any())
                    dropObject = m_SerializedProperty.boundsValue.center;
                else
                    dropObject = m_SerializedProperty.boundsValue.extents;
            }
            else if (inputFields.Length > 0) // If we've grabbed a single field
                dropObject = inputFields[0].text;
#endif

            return dropObject;
        }

        protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            return dropObject is string || dropObject is Bounds;
        }

        protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            var str = dropObject as string;
            if (str != null)
            {
                var inputField = fieldBlock.GetComponentInChildren<NumericInputField>();
                var index = Array.IndexOf(m_ExtentsFields, inputField);
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
            if (dropObject is Bounds)
            {
                m_SerializedProperty.boundsValue = (Bounds)dropObject;

                UpdateInputFields();

                FinalizeModifications();
            }
#endif
        }
    }
}
