#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HierarchyModule : MonoBehaviour, ISelectionChanged
	{
		readonly List<IUsesHierarchyData> m_HierarchyLists = new List<IUsesHierarchyData>();
		HierarchyData m_HierarchyData;
		HierarchyProperty m_HierarchyProperty;

		readonly List<IFilterUI> m_FilterUIs = new List<IFilterUI>();
		readonly HashSet<string> m_ObjectTypes = new HashSet<string>();

		void OnEnable()
		{
			EditorApplication.hierarchyWindowChanged += UpdateHierarchyData;
			UpdateHierarchyData();
		}

		void OnDisable()
		{
			EditorApplication.hierarchyWindowChanged -= UpdateHierarchyData;
		}

		public void OnSelectionChanged()
		{
			UpdateHierarchyData();
		}

		public void AddConsumer(IUsesHierarchyData consumer)
		{
			consumer.hierarchyData = GetHierarchyData();
			m_HierarchyLists.Add(consumer);
		}

		public void RemoveConsumer(IUsesHierarchyData consumer)
		{
			m_HierarchyLists.Remove(consumer);
		}

		public void AddConsumer(IFilterUI consumer)
		{
			consumer.filterList = GetFilterList();
			m_FilterUIs.Add(consumer);
		}

		public void RemoveConsumer(IFilterUI consumer)
		{
			m_FilterUIs.Remove(consumer);
		}

		List<string> GetFilterList()
		{
			return m_ObjectTypes.ToList();
		}

		List<HierarchyData> GetHierarchyData()
		{
			if (m_HierarchyData == null)
				return new List<HierarchyData>();

			return m_HierarchyData.children;
		}

		void UpdateHierarchyData()
		{
			m_ObjectTypes.Clear();

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

			var hasChanged = false;
			var hasNext = true;
			m_HierarchyData = CollectHierarchyData(ref hasNext, ref hasChanged, m_HierarchyData, m_HierarchyProperty, m_ObjectTypes);

			if (hasChanged)
			{
				foreach (var list in m_HierarchyLists)
				{
					list.hierarchyData = GetHierarchyData();
				}

				// Send new data to existing filterUIs
				foreach (var filterUI in m_FilterUIs)
				{
					filterUI.filterList = GetFilterList();
				}
			}
		}

		HierarchyData CollectHierarchyData(ref bool hasNext, ref bool hasChanged, HierarchyData hd, HierarchyProperty hp, HashSet<string> objectTypes)
		{
			var depth = hp.depth;
			var name = hp.name;
			var instanceID = hp.instanceID;
			var types = InstanceIDToComponentTypes(instanceID, objectTypes);

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
						types = InstanceIDToComponentTypes(instanceID, objectTypes);
					}

					if (i >= children.Count)
					{
						children.Add(CollectHierarchyData(ref hasNext, ref hasChanged, null, hp, objectTypes));
						hasChanged = true;
					}
					else if (children[i].index != hp.instanceID)
					{
						children[i] = CollectHierarchyData(ref hasNext, ref hasChanged, null, hp, objectTypes);
						hasChanged = true;
					}
					else
					{
						children[i] = CollectHierarchyData(ref hasNext, ref hasChanged, children[i], hp, objectTypes);
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
			{
				hasChanged = true;
			}

			if (hd != null)
			{
				hd.children = children;
				hd.name = name;
				hd.instanceID = instanceID;
				hd.types = types;
			}

			return hd ?? new HierarchyData(name, instanceID, types, children);
		}

		static HashSet<string> InstanceIDToComponentTypes(int instanceID, HashSet<string> allTypes)
		{
			var types = new HashSet<string>();
			var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (go)
			{
				var components = go.GetComponents<Component>();
				for (int i = 0; i < components.Length; i++)
				{
					var component = components[i];

					if (!component)
						continue;

					if (component is Transform)
						continue;

					var typeName = component.GetType().Name;
					if (component is MonoBehaviour)
						typeName = "MonoBehaviour";

					types.Add(typeName);
					allTypes.Add(typeName);
				}
			}
			return types;
		}
	}
}
#endif
