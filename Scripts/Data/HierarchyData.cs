#if UNITY_EDITOR
using ListView;
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
{
	sealed class HierarchyData : ListViewItemNestedData<HierarchyData>
	{
		const string k_TemplateName = "HierarchyListItem";

		public string name { get; set; }

		public int instanceID { get; set; }

		public bool locked { get; set; }

		public HierarchyData(string name, int instanceID, bool locked, List<HierarchyData> children = null)
		{
			template = k_TemplateName;
			this.name = name;
			this.instanceID = instanceID;
			this.locked = locked;
			m_Children = children;
		}
	}
}
#endif
