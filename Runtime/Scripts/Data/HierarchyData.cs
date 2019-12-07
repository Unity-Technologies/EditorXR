using System.Collections.Generic;
using Unity.Labs.ListView;
using UnityEditor;
using UnityEngine;

#if !UNITY_EDITOR
/// <summary>
/// The type of objects to display in the hierarchy
/// </summary>
public enum HierarchyType
{
    /// <summary>
    /// Display assets
    /// </summary>
    Assets = 1,

    /// <summary>
    /// Display GameObjects
    /// </summary>
    GameObjects = 2,

    /// <summary>
    /// Display packages
    /// </summary>
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

namespace Unity.Labs.EditorXR
{
    sealed class HierarchyData : NestedListViewItemData<HierarchyData, int>
    {
        const string k_TemplateName = "HierarchyListItem";

        readonly int m_Index;

        public string name { get; set; }
        public HashSet<string> types { get; set; }

        public override string template { get { return k_TemplateName; } }
        public override int index { get { return m_Index; } }

#if UNITY_EDITOR
        public GameObject gameObject { get { return (GameObject)EditorUtility.InstanceIDToObject(index); } }

        public HierarchyData(HierarchyProperty property)
        {
            name = property.name;
            // TODO: Hierarchy indices at runtime
            m_Index = property.instanceID;
        }
#else
        public GameObject gameObject { get { return null; } }
#endif
    }
}
