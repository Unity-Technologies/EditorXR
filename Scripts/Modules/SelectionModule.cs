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

		public GameObject GetSelectionCandidate(GameObject hoveredObject, bool useGrouping = false)
		{
			// If we can't even select the object we're starting with, then skip any further logic
			if (!CanSelectObject(hoveredObject, false))
				return null;

			// By default the selection candidate would be the same object passed in
			if (!useGrouping)
				return hoveredObject;

			// Only offer up the group root as the selection on first selection; Subsequent selections would allow children from the group
			var groupRoot = GetGroupRoot(hoveredObject);
			if (groupRoot && groupRoot != m_CurrentGroupRoot && CanSelectObject(groupRoot, false))
				return groupRoot;
			
			return hoveredObject;
		}

		bool CanSelectObject(GameObject hoveredObject, bool useGrouping)
		{
			if (isLocked(hoveredObject))
				return false;

			if (hoveredObject != null)
			{
				if (hoveredObject.isStatic)
					return false;

				if (useGrouping)
				{
					// Check the same rules on our selection candidate
					return CanSelectObject(GetSelectionCandidate(hoveredObject, true), false);
				}
			}

			return true;
		}

		public void SelectObject(GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGroupRoot = false)
		{
			var selection = GetSelectionCandidate(hoveredObject);
			
			if (useGroupRoot && selection != m_CurrentGroupRoot)
				m_CurrentGroupRoot = selection;
			m_SelectedObjects.Clear();

			// Multi-Select
			if (multiSelect)
			{
				m_SelectedObjects.AddRange(Selection.objects);
				if (m_SelectedObjects.Contains(selection))
				{
					// Already selected, so remove from selection
					m_SelectedObjects.Remove(selection);
				}
				else
				{
					// Add to selection
					m_SelectedObjects.Add(selection);
					Selection.activeObject = selection;
				}
			}
			else
			{
				m_SelectedObjects.Clear();
				Selection.activeObject = selection;
				m_SelectedObjects.Add(selection);
			}

			Selection.objects = m_SelectedObjects.ToArray();

			// Call selected with the source rayOrigin to show radial menu
			if (selected != null)
				selected(rayOrigin);
		}

		static GameObject GetGroupRoot(GameObject hoveredObject)
		{
			if (!hoveredObject)
				return null;

			var groupRoot = PrefabUtility.FindPrefabRoot(hoveredObject);
			if (!groupRoot)
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