using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	internal class HierarchyModule : MonoBehaviour
	{
		readonly List<IUsesHierarchyData> m_HierarchyLists = new List<IUsesHierarchyData>();
		HierarchyData m_HierarchyData;
#if UNITY_EDITOR
		HierarchyProperty m_HierarchyProperty;

		void OnEnable()
		{
			EditorApplication.hierarchyWindowChanged += UpdateHierarchyData;
			UpdateHierarchyData();
		}

		void OnDisable()
		{
			EditorApplication.hierarchyWindowChanged -= UpdateHierarchyData;
		}
#endif

		public void AddConsumer(IUsesHierarchyData consumer)
		{
			consumer.hierarchyData = GetHierarchyData();
			m_HierarchyLists.Add(consumer);
		}

		public void RemoveConsumer(IUsesHierarchyData consumer)
		{
			m_HierarchyLists.Remove(consumer);
		}

		List<HierarchyData> GetHierarchyData()
		{
			if (m_HierarchyData == null)
				return new List<HierarchyData>();

			return m_HierarchyData.children;
		}

		void UpdateHierarchyData()
		{
#if UNITY_EDITOR
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
#endif

			bool hasChanged = false;
#if UNITY_EDITOR
			var hasNext = true;
			m_HierarchyData = CollectHierarchyData(ref hasNext, ref hasChanged, m_HierarchyData, m_HierarchyProperty);
#endif

			if (hasChanged)
			{
				foreach (var list in m_HierarchyLists)
				{
					list.hierarchyData = GetHierarchyData();
				}
			}
		}

#if UNITY_EDITOR
		HierarchyData CollectHierarchyData(ref bool hasNext, ref bool hasChanged, HierarchyData hd, HierarchyProperty hp)
		{
			var depth = hp.depth;
			var name = hp.name;
			var instanceID = hp.instanceID;

			List<HierarchyData> children = null;
			if (hp.hasChildren)
			{
				if (hd != null && hd.children == null)
					hasChanged = true;

				children = hd == null || hd.children == null ? new List<HierarchyData>() : hd.children;

				hasNext = hp.Next(null);
				var i = 0;
				while (hasNext && hp.depth > depth)
				{
					var go = EditorUtility.InstanceIDToObject(hp.instanceID);

					if (go == gameObject)
					{
						// skip children of EVR to prevent the display of EVR contents
						while (hp.Next(null) && hp.depth > depth + 1) { }

						// If EVR is the last object, don't add anything to the list
						if (hp.instanceID == 0)
							break;

						name = hp.name;
						instanceID = hp.instanceID;
					}

					if (i >= children.Count)
					{
						children.Add(CollectHierarchyData(ref hasNext, ref hasChanged, null, hp));
						hasChanged = true;
					}
					else if (children[i].index != hp.instanceID)
					{
						children[i] = CollectHierarchyData(ref hasNext, ref hasChanged, null, hp);
						hasChanged = true;
					}
					else
					{
						children[i] = CollectHierarchyData(ref hasNext, ref hasChanged, children[i], hp);
					}

					if (hasNext)
						hasNext = hp.Next(null);

					i++;
				}

				if (i != children.Count)
				{
					children.RemoveRange(i, children.Count - i);
					hasChanged = true;
				}

				if (children.Count == 0)
					children = null;

				if (hasNext)
					hp.Previous(null);
			}
			else if (hd != null && hd.children != null)
				hasChanged = true;

			if (hd != null)
			{
				hd.children = children;
				hd.name = name;
				hd.instanceID = instanceID;
			}

			return hd ?? new HierarchyData(name, instanceID, children);
		}
#endif
	}
}