using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    [CustomPropertyDrawer(typeof(FlagsPropertyAttribute))]
    sealed class FlagsPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.intValue = UIUtils.MaskField(position, label, property.intValue, property.enumNames, UIUtils.SerializedPropertyToType(property));
        }
    }
}
