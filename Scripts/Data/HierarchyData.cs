#if UNITY_EDITOR
using ListView;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	sealed class HierarchyData : ListViewItemNestedData<HierarchyData, int>
	{
		const string k_TemplateName = "HierarchyListItem";

		public string name { get; set; }

		public override int index
		{
			get { return instanceID; }
		}
		public int instanceID { private get; set; }

		public GameObject gameObject { get { return (GameObject)EditorUtility.InstanceIDToObject(instanceID); } }

		public HashSet<string> types { get; set; }

		public HierarchyData(string name, int instanceID, HashSet<string> types, List<HierarchyData> children = null)
		{
			template = k_TemplateName;
			this.name = name;
			this.instanceID = instanceID;
			this.types = types;
			m_Children = children;
		}
	}
}
#endif
