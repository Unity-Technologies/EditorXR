namespace UnityEngine.VR.Utilities
{
	using System;
	using UnityEngine;
#if UNITY_EDITOR
	using UnityEditor;
	using System.Reflection;
	using Modules;
#endif

	/// <summary>
	/// EditorVR Utilities
	/// </summary>
	public static partial class U
	{
		/// <summary>
		/// Object related EditorVR utilities
		/// </summary>
		public static class UI
		{
			private const float kDoubleClickIntervalMax = 0.3f;
			private const float kDoubleClickIntervalMin = 0.15f;

			public static bool DoubleClick(float timeSinceLastClick)
			{
				return timeSinceLastClick <= kDoubleClickIntervalMax && timeSinceLastClick >= kDoubleClickIntervalMin;
			}

			public static bool IsDirectEvent(RayEventData eventData)
			{
				return eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.distance <= eventData.pointerLength;
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
			/// Special version of EditorGUI.MaskField which ensures that only the chosen bits are set. Existing MaskFiled has a bug
			/// whereby the first bit of the value may or may not be set depending on how the UI was used.
			/// </summary>
			/// <returns></returns>
			public static int MaskField(Rect position, GUIContent label, int mask, string[] displayedOptions, Type propertyType)
			{
				mask = EditorGUI.MaskField(position, label, mask, displayedOptions);
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

			public static Type SerializedPropertyToType(SerializedProperty property)
			{
				string[] parts = property.propertyPath.Split('.');

				Type currentType = property.serializedObject.targetObject.GetType();

				for (int i = 0; i < parts.Length; i++)
					currentType = currentType.GetField(parts[i], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance).FieldType;

				return currentType;
			}
		}
	}
}