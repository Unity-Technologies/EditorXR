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

		public GameObject GetSelectObject(GameObject hoveredObject)
		{
			if (hoveredObject != null)
			{
				if (hoveredObject.isStatic)
					return null;

				GameObject newPrefabRoot;
				hoveredObject = CheckGroupRoot(hoveredObject, out newPrefabRoot);
			}

			// Do this after checking for a prefab so that we check if the prefab is locked
			if (isLocked(hoveredObject))
				return null;

			return hoveredObject;
		}
		/// <summary>
		/// Select an object
		/// </summary>
		/// <param name="hoveredObject">The hovered object we want to select. We might select its prefab root if there is one</param>
		/// <param name="rayOrigin">The rayOrigin used to make the selection</param>
		/// <param name="multiSelect">Whether this will be part of a multiple selection</param>
		/// <returns>The selected GameObject, or null if nothing was selected. This will be the prefab root if that's what we selected</returns>
		public void SelectObject(GameObject hoveredObject, Transform rayOrigin, bool multiSelect)
		{
			if (isLocked(hoveredObject))
				return;

			// Select the prefab root if we don't already have one selected
			GameObject groupRoot = null;
			if (hoveredObject != null)
			{
				if (hoveredObject.isStatic)
					return;

				hoveredObject = CheckGroupRoot(hoveredObject, out groupRoot);
			}

			m_CurrentGroupRoot = groupRoot;
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

		GameObject CheckGroupRoot(GameObject hoveredObject, out GameObject groupRoot)
		{
			// If gameObject is within a prefab and not the current prefab, choose prefab root
			groupRoot = PrefabUtility.FindPrefabRoot(hoveredObject);
			if (groupRoot && groupRoot != hoveredObject)
			{
				if (groupRoot != m_CurrentGroupRoot)
					hoveredObject = groupRoot;
			}
			else
			{
				groupRoot = GetGroupRoot(hoveredObject.transform).gameObject;
				if (groupRoot && groupRoot != m_CurrentGroupRoot)
					hoveredObject = groupRoot;
			}
			return hoveredObject;
		}

		static Transform GetGroupRoot(Transform transform)
		{
			var parent = transform.parent;
			if (parent)
			{
				if (parent.GetComponent<Renderer>())
					return GetGroupRoot(parent);

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