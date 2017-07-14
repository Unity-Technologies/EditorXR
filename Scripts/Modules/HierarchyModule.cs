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
		readonly List<HierarchyData> m_HierarchyData = new List<HierarchyData>();
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
			return m_HierarchyData ?? new List<HierarchyData>();
		}

		void UpdateHierarchyData()
		{
			m_ObjectTypes.Clear();
			m_HierarchyData.Clear();

			if (m_HierarchyProperty == null)
				m_HierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
			else
				m_HierarchyProperty.Reset();

			var lastDepth = 0;
			var stack = new Stack<HierarchyData>();
			while (m_HierarchyProperty.Next(null))
			{
				var instanceID = m_HierarchyProperty.instanceID;
				var go = EditorUtility.InstanceIDToObject(instanceID);
				var currentDepth = m_HierarchyProperty.depth;
				if (go == gameObject)
				{
					var depth = currentDepth;
					// skip children of EVR to prevent the display of EVR contents
					while (m_HierarchyProperty.Next(null) && m_HierarchyProperty.depth > depth) { }

					currentDepth = m_HierarchyProperty.depth;
					instanceID = m_HierarchyProperty.instanceID;
					// If EVR is the last object, early out
					if (instanceID == 0)
						break;
				}

				var types = InstanceIDToComponentTypes(instanceID, m_ObjectTypes);
				var currentHierarchyData = new HierarchyData(m_HierarchyProperty, types);

				HierarchyData parent = null;
				if (currentDepth <= lastDepth)
				{
					// Add one to pop off last sibling
					var count = lastDepth - currentDepth + 1;
					while (count-- > 0 && stack.Count > 0)
					{
						stack.Pop();
					}
				}

				if (stack.Count > 0)
					parent = stack.Peek();

				if (parent != null)
				{
					if (parent.children == null)
						parent.children = new List<HierarchyData>();
					parent.children.Add(currentHierarchyData);
				}
				else
				{
					m_HierarchyData.Add(currentHierarchyData);
				}

				stack.Push(currentHierarchyData);
				lastDepth = currentDepth;
			}

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

		static HashSet<string> InstanceIDToComponentTypes(int instanceID, HashSet<string> allTypes)
		{
			var types = new HashSet<string>();
			var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (go)
			{
				var components = go.GetComponents<Component>();
				foreach (var component in components)
				{
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
