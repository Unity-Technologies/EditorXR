using System.Collections.Generic;
using Unity.Labs.ListView;
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

    public void Next() { }
}
#endif

namespace UnityEditor.Experimental.EditorVR
{
    sealed class HierarchyData : NestedListViewItemData<HierarchyData, int>
    {
        const string k_TemplateName = "HierarchyListItem";

        public string name { get; set; }

        public HashSet<string> types { get; set; }

#if UNITY_EDITOR
        public GameObject gameObject { get { return (GameObject)EditorUtility.InstanceIDToObject(index); } }

        public HierarchyData(HierarchyProperty property)
        {
            template = k_TemplateName;
            name = property.name;
            // TODO: Hierarchy indices at runtime
            index = property.instanceID;
        }
#else
        public GameObject gameObject { get { return null; } }
#endif
    }
}
