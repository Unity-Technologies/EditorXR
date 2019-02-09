using System;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    /// <summary>
    /// UI related utilities
    /// </summary>
    static class UIUtils
    {
        /// <summary>
        /// Maximum interval between clicks that count as a double-click
        /// </summary>
        public const float DoubleClickIntervalMax = 0.3f;

        const float k_DoubleClickIntervalMin = 0.15f;

        /// <summary>
        /// Returns whether the given time interval qualifies as a double-click
        /// </summary>
        /// <param name="timeSinceLastClick">Time interval between clicks</param>
        /// <returns></returns>
        public static bool IsDoubleClick(float timeSinceLastClick)
        {
            return timeSinceLastClick <= DoubleClickIntervalMax && timeSinceLastClick >= k_DoubleClickIntervalMin;
        }

        public static bool IsDirectEvent(RayEventData eventData)
        {
            return eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.distance <= eventData.pointerLength || eventData.dragging;
        }

        public static bool IsValidEvent(RayEventData eventData, SelectionFlags selectionFlags)
        {
            if ((selectionFlags & SelectionFlags.Direct) != 0 && IsDirectEvent(eventData))
                return true;

            if ((selectionFlags & SelectionFlags.Ray) != 0)
                return true;

            return false;
        }

        /// <summary>
        /// Special version of EditorGUI.MaskField which ensures that only the chosen bits are set. We need this version of the
        /// function to check explicitly whether only a single bit was set.
        /// </summary>
        /// <returns></returns>
        public static int MaskField(Rect position, GUIContent label, int mask, string[] displayedOptions, Type propertyType)
        {
#if UNITY_EDITOR
            mask = EditorGUI.MaskField(position, label, mask, displayedOptions);
#endif
            return ActualEnumFlags(mask, propertyType);
        }

        public static int ActualEnumFlags(int value, Type t)
        {
            if (value < 0)
            {
                int bits = 0;
                foreach (var enumValue in System.Enum.GetValues(t))
                {
                    int checkBit = value & (int)enumValue;
                    if (checkBit != 0)
                    {
                        bits |= (int)enumValue;
                    }
                }
                value = bits;
            }
            return value;
        }

#if UNITY_EDITOR
        public static Type SerializedPropertyToType(SerializedProperty property)
        {
            var parts = property.propertyPath.Split('.');

            var currentType = property.serializedObject.targetObject.GetType();

            if (parts.Length == 0)
                return null;

            var field = GetFieldInTypeOrParent(currentType, parts[parts.Length - 1]);

            return field != null ? field.FieldType : null;
        }
#endif

        public static FieldInfo GetFieldInTypeOrParent(Type type, string fieldName)
        {
            while (true)
            {
                if (type == null)
                    return null;
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
                if (field != null) return field;
                type = type.BaseType;
            }
        }
    }
}
