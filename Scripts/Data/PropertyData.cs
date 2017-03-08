#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR.Data
{
	sealed class PropertyData : InspectorData
	{
#if UNITY_EDITOR
		public SerializedProperty property
		{
			get { return m_Property; }
		}
		readonly SerializedProperty m_Property;

		public override int index
		{
			get { return property.GetHashCode(); }
		}

		public PropertyData(string template, SerializedObject serializedObject, List<InspectorData> children, SerializedProperty property)
			: base(template, serializedObject, children)
		{
			m_Property = property;
		}
#endif
	}
}
#endif
