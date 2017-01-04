using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	public class SelectionModule : MonoBehaviour, IUsesGameObjectLocking, ISelectionChanged
	{
		GameObject m_CurrentGroupRoot;
		readonly List<Object> m_SelectedObjects = new List<Object>(); // Keep the list to avoid allocations--we do not use it to maintain state

		public Action<GameObject, bool> setLocked { private get; set; }
		public Func<GameObject, bool> isLocked { private get; set; }

		public event Action<Transform> selected;

		public bool CanSelectObject(GameObject hoveredObject, bool useGroupRoot = false)
		{
			if (isLocked(hoveredObject))
				return false;

			if (hoveredObject != null)
			{
				if (hoveredObject.isStatic)
					return false;

				var groupRoot = GetGroupRoot(hoveredObject);
				if (groupRoot && (groupRoot.isStatic || isLocked(groupRoot)))
					return false;
			}

			return true;
		}

		public void SelectObject(GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGroupRoot)
		{
			if (!CanSelectObject(hoveredObject, useGroupRoot))
				return;

			if (useGroupRoot)
			{
				var groupRoot = GetGroupRoot(hoveredObject);

				if (groupRoot != m_CurrentGroupRoot)
					hoveredObject = groupRoot;
				else
				{
					if (groupRoot && groupRoot != m_CurrentGroupRoot)
						hoveredObject = groupRoot;
				}

				if (hoveredObject != null && hoveredObject != m_CurrentGroupRoot)
					m_CurrentGroupRoot = groupRoot;
			}
			m_SelectedObjects.Clear();

			// Multi-Select
			if (multiSelect)
			{
				m_SelectedObjects.AddRange(Selection.objects);
				if (m_SelectedObjects.Contains(hoveredObject))
				{
					// Already selected, so remove from selection
					m_SelectedObjects.Remove(hoveredObject);
				}
				else
				{
					// Add to selection
					m_SelectedObjects.Add(hoveredObject);
					Selection.activeObject = hoveredObject;
				}
			}
			else
			{
				m_SelectedObjects.Clear();
				Selection.activeObject = hoveredObject;
				m_SelectedObjects.Add(hoveredObject);
			}

			Selection.objects = m_SelectedObjects.ToArray();

			// Call selected with the source rayOrigin to show radial menu
			if (selected != null)
				selected(rayOrigin);
		}

		GameObject GetGroupRoot(GameObject hoveredObject)
		{
			var groupRoot = PrefabUtility.FindPrefabRoot(hoveredObject);
			if (!groupRoot || groupRoot == hoveredObject)
				groupRoot = FindGroupRoot(hoveredObject.transform).gameObject;

			return groupRoot;
		}

		static Transform FindGroupRoot(Transform transform)
		{
			var parent = transform.parent;
			if (parent)
			{
				if (parent.GetComponent<Renderer>())
					return FindGroupRoot(parent);

				return parent;
			}


			return transform;
		}

		public void OnSelectionChanged()
		{
			// Clear prefab root if selection is cleared (m_SelectedObjects clears itself in SelectObject)
			if (Selection.objects.Length == 0)
				m_CurrentGroupRoot = null;
		}
	}
}