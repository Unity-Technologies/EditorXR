#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
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

			if (m_HierarchyProperty == null)
				m_HierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
			else
				m_HierarchyProperty.Reset();

			var hasChanged = false;
			var lastDepth = 0;
			var dataStack = new Stack<HierarchyData>();
			var siblingIndexStack = new Stack<int>();
			dataStack.Push(null);
			siblingIndexStack.Push(0);
			while (m_HierarchyProperty.Next(null))
			{
				var instanceID = m_HierarchyProperty.instanceID;
				var types = InstanceIDToComponentTypes(instanceID, m_ObjectTypes);
				var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
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

				if (go && (go.GetComponent<InputManager>() || go.GetComponent<EditingContextManager>()))
					continue;

				if (currentDepth <= lastDepth)
				{
					if (dataStack.Count > 1) // Pop off last sibling
					{
						if (CleanUpHierarchyData(dataStack.Pop(), siblingIndexStack.Pop()))
							hasChanged = true;
					}

					var count = lastDepth - currentDepth;
					while (count-- > 0)
					{
						if (CleanUpHierarchyData(dataStack.Pop(), siblingIndexStack.Pop()))
							hasChanged = true;
					}
				}

				var parent = dataStack.Peek();
				var siblingIndex = siblingIndexStack.Pop();

				if (parent != null && parent.children == null)
					parent.children = new List<HierarchyData>();

				var children = parent == null ? m_HierarchyData : parent.children;

				HierarchyData currentHierarchyData;
				if (siblingIndex >= children.Count)
				{
					currentHierarchyData = new HierarchyData(m_HierarchyProperty, types);
					children.Add(currentHierarchyData);
					hasChanged = true;
				}
				else if (children[siblingIndex].index != instanceID)
				{
					currentHierarchyData = new HierarchyData(m_HierarchyProperty, types);
					children[siblingIndex] = currentHierarchyData;
					hasChanged = true;
				}
				else
				{
					currentHierarchyData = children[siblingIndex];

					if (!currentHierarchyData.types.SetEquals(types))
						hasChanged = true;

					currentHierarchyData.types = types; // In case of added components
				}

				dataStack.Push(currentHierarchyData);
				siblingIndexStack.Push(siblingIndex + 1);
				siblingIndexStack.Push(0);
				lastDepth = currentDepth;
			}

			while (siblingIndexStack.Count > 0 && dataStack.Count > 0)
			{
				if (CleanUpHierarchyData(dataStack.Pop(), siblingIndexStack.Pop()))
					hasChanged = true;
			}

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

		bool CleanUpHierarchyData(HierarchyData data, int lastSiblingIndex)
		{
			var children = data == null ? m_HierarchyData : data.children;
			var childrenCount = children == null ? 0 : children.Count;
			if (children != null && lastSiblingIndex < childrenCount)
			{
				children.RemoveRange(lastSiblingIndex, childrenCount - lastSiblingIndex);
				if (data != null && children.Count == 0)
					data.children = null;

				return true;
			}

			return false;
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
