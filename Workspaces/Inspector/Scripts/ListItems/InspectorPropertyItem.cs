using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	internal abstract class InspectorPropertyItem : InspectorListItem
	{
		[SerializeField]
		Text m_Label;

#if UNITY_EDITOR
		protected SerializedProperty m_SerializedProperty;

		public override void Setup(InspectorData data)
		{
			base.Setup(data);

			m_SerializedProperty = ((PropertyData)data).property;

			m_Label.text = m_SerializedProperty.displayName;
		}
#endif
	}
}