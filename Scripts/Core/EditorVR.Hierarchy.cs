#if UNITY_EDITORVR
using System.Collections.Generic;
using UnityEngine.Experimental.EditorVR;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
#if UNITY_EDITOR
		readonly List<IUsesHierarchyData> m_HierarchyLists = new List<IUsesHierarchyData>();
		HierarchyData m_HierarchyData;
		HierarchyProperty m_HierarchyProperty;
#endif

		// TODO: Find a better callback for when objects are created or destroyed
		void OnHierarchyChanged()
		{
			m_MiniWorldIgnoreListDirty = true;
			m_PixelRaycastIgnoreListDirty = true;

			UpdateHierarchyData();
		}

		List<HierarchyData> GetHierarchyData()
		{
			if (m_HierarchyData == null)
				return new List<HierarchyData>();

			return m_HierarchyData.children;
		}

		void UpdateHierarchyData()
		{
			if (m_HierarchyProperty == null)
			{
				m_HierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
				m_HierarchyProperty.Next(null);
			}
			else
			{
				m_HierarchyProperty.Reset();
				m_HierarchyProperty.Next(null);
			}

			var hasNext = true;
			bool hasChanged = false;
			m_HierarchyData = CollectHierarchyData(ref hasNext, ref hasChanged, m_HierarchyData, m_HierarchyProperty);

			if (hasChanged)
			{
				foreach (var list in m_HierarchyLists)
				{
					list.hierarchyData = GetHierarchyData();
				}
			}
		}

		HierarchyData CollectHierarchyData(ref bool hasNext, ref bool hasChanged, HierarchyData hd, HierarchyProperty hp)
		{
			var depth = hp.depth;
			var name = hp.name;
			var instanceID = hp.instanceID;

			List<HierarchyData> list = null;
			list = (hd == null || hd.children == null) ? new List<HierarchyData>() : hd.children;

			if (hp.hasChildren)
			{
				hasNext = hp.Next(null);
				var i = 0;
				while (hasNext && hp.depth > depth)
				{
					var go = EditorUtility.InstanceIDToObject(hp.instanceID);

					if (go == gameObject)
					{
						// skip children of EVR to prevent the display of EVR contents
						while (hp.Next(null) && hp.depth > depth + 1) { }
						name = hp.name;
						instanceID = hp.instanceID;
					}

					if (i >= list.Count)
					{
						list.Add(CollectHierarchyData(ref hasNext, ref hasChanged, null, hp));
						hasChanged = true;
					}
					else if (list[i].instanceID != hp.instanceID)
					{
						list[i] = CollectHierarchyData(ref hasNext, ref hasChanged, null, hp);
						hasChanged = true;
					}
					else
					{
						list[i] = CollectHierarchyData(ref hasNext, ref hasChanged, list[i], hp);
					}

					if (hasNext)
						hasNext = hp.Next(null);

					i++;
				}

				if (i != list.Count)
				{
					list.RemoveRange(i, list.Count - i);
					hasChanged = true;
				}

				if (hasNext)
					hp.Previous(null);
			}
			else
			{
				list.Clear();
			}

			List<HierarchyData> children = null;
			if (list.Count > 0)
				children = list;

			if (hd != null)
			{
				hd.children = children;
				hd.name = name;
				hd.instanceID = instanceID;
			}

			return hd ?? new HierarchyData(name, instanceID, children);
		}
	}
}
#endif
