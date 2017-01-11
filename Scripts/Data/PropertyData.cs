using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.EditorVR.Data
{
	public class PropertyData : InspectorData
	{
#if UNITY_EDITOR
		public SerializedProperty property { get; private set; }

		public override int instanceID
		{
			get { return property.GetHashCode(); }
		}

		public PropertyData(string template, SerializedObject serializedObject, List<InspectorData> children, SerializedProperty property)
			: base(template, serializedObject, children)
		{
			this.property = property;
		}
#endif
	}
}