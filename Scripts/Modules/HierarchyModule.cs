#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HierarchyModule : MonoBehaviour, ISelectionChanged
	{
		readonly string[] k_IgnoredTypes = { "InputManager", "EditingContextManager" };

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

			//Debug.Log("update");

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
				var go = EditorUtility.InstanceIDToObject(instanceID);
				var currentDepth = m_HierarchyProperty.depth;
				if (go == gameObject || types.Overlaps(k_IgnoredTypes))
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

				//if (currentDepth > lastDepth)
				//	siblingIndexStack.Push(0);

				if (currentDepth <= lastDepth)
				{
					//Debug.Log(dataStack.Count);
					if (dataStack.Count > 1) // Pop off last sibling
					{
						if (CleanUpHierarchyData(dataStack.Pop(), siblingIndexStack.Pop()))
							hasChanged = true;
					}

					//if (currentDepth < lastDepth)
					//{
					//	if (CleanUpHierarchyData(dataStack.Peek(), siblingIndexStack.Peek()))
					//		hasChanged = true;
					//	//var lastParent = dataStack.Peek();
					//	//var lastChildren = lastParent == null ? m_HierarchyData : lastParent.children;
					//	//var lastSiblingIndex = siblingIndexStack.Peek();
					//	//var childrenCount = lastChildren.Count;
					//	//if (lastSiblingIndex != childrenCount)
					//	//{
					//	//	lastChildren.RemoveRange(lastSiblingIndex, childrenCount - lastSiblingIndex);
					//	//	hasChanged = true;
					//	//}
					//}

					var count = lastDepth - currentDepth;
					while (count-- > 0)
					{
						if (CleanUpHierarchyData(dataStack.Pop(), siblingIndexStack.Pop()))
							hasChanged = true;
					}
				}

				var parent = dataStack.Peek();
				var siblingIndex = siblingIndexStack.Pop();

				//var log = m_HierarchyProperty.name;
				//for (var i = 0; i < m_HierarchyProperty.depth; i++)
				//{
				//	log = "    " + log;
				//}
				//Debug.Log(lastDepth + " - " + currentDepth + "(" + siblingIndex + "): " + log);

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

				//var siblingIndex = siblingIndexStack.Pop();
				//HierarchyData parent = null;
				//if (dataStack.Count > 0)
				//	parent = dataStack.Pop();
				//Debug.Log(siblingIndex + ", " + (parent == null ? "no parent" : parent.name));
				//var children = parent == null ? m_HierarchyData : parent.children;
				//if (children != null)
				//{
				//	var childrenCount = children.Count;
				//	Debug.Log(siblingIndex + ", " + childrenCount);
				//	if (siblingIndex != childrenCount)
				//	{
				//		children.RemoveRange(siblingIndex, childrenCount - siblingIndex);
				//		hasChanged = true;
				//	}

				//	if (parent != null && children.Count == 0)
				//		parent.children = null;
				//}
			}

			//foreach (var hierarchyData in m_HierarchyData)
			//{
			//	hierarchyData.Print();
			//}

			if (hasChanged)
			{
				Debug.Log("change");
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

		//HierarchyData CollectHierarchyData(HierarchyProperty hp, HashSet<string> objectTypes)
		//{
		//	var depth = hp.depth;
		//	var name = hp.name;
		//	var instanceID = hp.instanceID;
		//	var types = InstanceIDToComponentTypes(instanceID, objectTypes);

		//	Stack<HierarchyData> stack = new Stack<HierarchyData>();
		//	if (hp.hasChildren)
		//	{
		//		if (hd != null && hd.children == null)
		//			hasChanged = true;

		//		children = hd == null || hd.children == null ? new List<HierarchyData>() : hd.children;

		//		hasNext = hp.Next(null);
		//		var i = 0;
		//		while (hasNext && hp.depth > depth)
		//		{
		//			var go = EditorUtility.InstanceIDToObject(hp.instanceID);

		//			if (go == gameObject)
		//			{
		//				// skip children of EVR to prevent the display of EVR contents
		//				while (hp.Next(null) && hp.depth > depth + 1) { }

		//				// If EVR is the last object, don't add anything to the list
		//				if (hp.instanceID == 0)
		//					break;

		//				name = hp.name;
		//				instanceID = hp.instanceID;
		//				types = InstanceIDToComponentTypes(instanceID, objectTypes);
		//			}

		//			if (i >= children.Count)
		//			{
		//				children.Add(CollectHierarchyData(ref hasNext, ref hasChanged, null, hp, objectTypes));
		//				hasChanged = true;
		//			}
		//			else if (children[i].index != hp.instanceID)
		//			{
		//				children[i] = CollectHierarchyData(ref hasNext, ref hasChanged, null, hp, objectTypes);
		//				hasChanged = true;
		//			}
		//			else
		//			{
		//				children[i] = CollectHierarchyData(ref hasNext, ref hasChanged, children[i], hp, objectTypes);
		//			}

		//			if (hasNext)
		//				hasNext = hp.Next(null);

		//			i++;
		//		}

		//		if (i != children.Count)
		//		{
		//			children.RemoveRange(i, children.Count - i);
		//			hasChanged = true;
		//		}

		//		if (children.Count == 0)
		//			children = null;

		//		if (hasNext)
		//			hp.Previous(null);
		//	}
		//	else if (hd != null && hd.children != null)
		//	{
		//		hasChanged = true;
		//	}

		//	if (hd != null)
		//	{
		//		hd.children = children;
		//		hd.name = name;
		//		hd.instanceID = instanceID;
		//		hd.types = types;
		//	}

		//	return hd ?? new HierarchyData(name, instanceID, types, children);
		//}

		bool CleanUpHierarchyData(HierarchyData data, int lastSiblingIndex)
		{
			var children = data == null ? m_HierarchyData : data.children;
			var childrenCount = children == null ? 0 : children.Count;
			//Debug.Log(name + ", " + childrenCount + " - " + lastSiblingIndex);

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
