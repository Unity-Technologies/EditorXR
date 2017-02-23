using ListView;
using System.Collections.Generic;

namespace UnityEngine.Experimental.EditorVR
{
	sealed class HierarchyData : ListViewItemNestedData<HierarchyData, int>
	{
		const string kTemplateName = "HierarchyListItem";

		public string name { get; set; }

		public override int index { get { return instanceID; } }
		public int instanceID { private get; set; }

		public HierarchyData(string name, int instanceID, List<HierarchyData> children = null)
		{
			template = kTemplateName;
			this.name = name;
			this.instanceID = instanceID;
			m_Children = children;
		}
	}
}