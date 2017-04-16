#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class SelectionModule : MonoBehaviour, IUsesGameObjectLocking, ISelectionChanged
	{
		GameObject m_CurrentGroupRoot;
		readonly List<Object> m_SelectedObjects = new List<Object>(); // Keep the list to avoid allocations--we do not use it to maintain state

		public Func<GameObject, GameObject> getGroupRoot { private get; set; }
		public Func<GameObject, bool> overrideSelectObject { private get; set; }

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
			var groupRoot = getGroupRoot(hoveredObject);
			if (groupRoot && groupRoot != m_CurrentGroupRoot && CanSelectObject(groupRoot, false))
				return groupRoot;
			
			return hoveredObject;
		}

		bool CanSelectObject(GameObject hoveredObject, bool useGrouping)
		{
			if (this.IsLocked(hoveredObject))
				return false;

			if (hoveredObject != null)
			{
				if (useGrouping)
					return CanSelectObject(GetSelectionCandidate(hoveredObject, true), false);
			}

			return true;
		}

		public void SelectObject(GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGrouping = false)
		{
			if (overrideSelectObject(hoveredObject))
				return;

			var selection = GetSelectionCandidate(hoveredObject, useGrouping);

			var groupRoot = getGroupRoot(hoveredObject);
			if (useGrouping && groupRoot != m_CurrentGroupRoot)
				m_CurrentGroupRoot = groupRoot;

			m_SelectedObjects.Clear();

			// Multi-Select
			if (multiSelect)
			{
				m_SelectedObjects.AddRange(Selection.objects);
				// Re-selecting an object removes it from selection
				if (!m_SelectedObjects.Remove(selection))
				{
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
			if (selected != null)
				selected(rayOrigin);
		}

		public void OnSelectionChanged()
		{
			// Selection can change outside of this module, so stay in sync
			if (Selection.objects.Length == 0)
				m_CurrentGroupRoot = null;
		}
	}
}
#endif
