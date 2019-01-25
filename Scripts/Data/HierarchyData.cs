using ListView;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_EDITOR
  public enum HierarchyType
  {
    Assets = 1,
    GameObjects = 2,
    Packages = 3,
  }

class HierarchyProperty
{
    public string name;
    public int instanceID;

    public HierarchyProperty(HierarchyType type)
    {
    }

    public void Next() {}
}
#endif

namespace UnityEditor.Experimental.EditorVR
{
    sealed class HierarchyData : ListViewItemNestedData<HierarchyData, int>
    {
        const string k_TemplateName = "HierarchyListItem";

        public string name { get; set; }

        public HashSet<string> types { get; set; }

#if UNITY_EDITOR
        public override int index
        {
            get { return instanceID; }
        }

        public int instanceID { private get; set; }

        public GameObject gameObject { get { return (GameObject)EditorUtility.InstanceIDToObject(instanceID); } }

        public HierarchyData(HierarchyProperty property)
        {
            template = k_TemplateName;
            name = property.name;
            instanceID = property.instanceID;
        }
#else
        // TODO: Hierarchy indices at runtime
        public override int index { get; protected set; }

        public GameObject gameObject { get { return null; } }
#endif
    }
}
