using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	public class SelectionTool : MonoBehaviour, ITool, IRay, IRaycaster, ICustomActionMap, IHighlight
	{
		private static HashSet<GameObject> s_SelectedObjects = new HashSet<GameObject>(); // Selection set is static because multiple selection tools can simulataneously add and remove objects from a shared selection

		private GameObject m_HoverGameObject;
		private GameObject m_PressedObject;
		private DateTime m_LastSelectTime;

		// The prefab (if any) that was double clicked, whose individual pieces can be selected
		private static GameObject s_CurrentPrefabOpened;

		public ActionMap actionMap { get { return m_ActionMap; } }
		[SerializeField]
		private ActionMap m_ActionMap;

		public ActionMapInput actionMapInput
		{
			get { return m_SelectionInput; }
			set { m_SelectionInput = (SelectionInput)value; }
		}
		private SelectionInput m_SelectionInput;

		public Func<Transform, GameObject> getFirstGameObject { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action<GameObject, bool> setHighlight { private get; set; }
		public Node selfNode { get; set; }
		public Node? node { private get; set; }

		public event Action<Node?> selected = delegate {};

		void Update()
		{
			var newHoverGameObject = getFirstGameObject(rayOrigin);
			GameObject newPrefabRoot = null;

			if (newHoverGameObject != null)
			{
				// If gameObject is within a prefab and not the current prefab, choose prefab root
				newPrefabRoot = PrefabUtility.FindPrefabRoot(newHoverGameObject);
				if (newPrefabRoot)
				{
					if (newPrefabRoot != s_CurrentPrefabOpened)
						newHoverGameObject = newPrefabRoot;
				}
			}
			// Handle changing highlight
			if (newHoverGameObject != m_HoverGameObject)
			{
				if (m_HoverGameObject != null)
					setHighlight(m_HoverGameObject, false);

				if (newHoverGameObject != null)
					setHighlight(newHoverGameObject, true);
			}

			m_HoverGameObject = newHoverGameObject;

			if (m_SelectionInput.select.wasJustPressed && m_HoverGameObject)
				m_PressedObject = m_HoverGameObject;
			// Handle select button press
			if (m_SelectionInput.select.wasJustReleased)
			{
				if (m_PressedObject == m_HoverGameObject)
				{
					s_CurrentPrefabOpened = newPrefabRoot;

					// Multi-Select
					if (m_SelectionInput.multiSelect.isHeld)
					{
						if (s_SelectedObjects.Contains(m_HoverGameObject))
						{
							// Already selected, so remove from selection
							s_SelectedObjects.Remove(m_HoverGameObject);
						}
						else
						{
							// Add to selection
							s_SelectedObjects.Add(m_HoverGameObject);
							Selection.activeGameObject = m_HoverGameObject;
						}
					}
					else
					{
						if (s_CurrentPrefabOpened && s_CurrentPrefabOpened != m_HoverGameObject)
							s_SelectedObjects.Remove(s_CurrentPrefabOpened);

						s_SelectedObjects.Clear();
						Selection.activeGameObject = m_HoverGameObject;
						s_SelectedObjects.Add(m_HoverGameObject);
					}

					Selection.objects = s_SelectedObjects.ToArray();
					selected(node);
				}

				m_PressedObject = null;
			}
		}

		void OnDisable()
		{
			if (m_HoverGameObject != null)
			{
				setHighlight(m_HoverGameObject, false);
				m_HoverGameObject = null;
			}
		}
	}
}
