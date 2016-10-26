using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputNew;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Tools
{
	public class SelectionTool : MonoBehaviour, ITool, IRay, IRaycaster, ICustomActionMap, IHighlight, IMenuOrigins, ILocking
	{
		private static HashSet<GameObject> s_SelectedObjects = new HashSet<GameObject>(); // Selection set is static because multiple selection tools can simulataneously add and remove objects from a shared selection

		private GameObject m_HoverGameObject;
		private DateTime m_LastSelectTime;

		// The prefab (if any) that was double clicked, whose individual pieces can be selected
		private static GameObject s_CurrentPrefabOpened; 

		public ActionMap actionMap { get { return m_ActionMap; } }
		[SerializeField]
		private ActionMap m_ActionMap;

		public Func<Transform, GameObject> getFirstGameObject { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action<GameObject, bool> setHighlight { private get; set; }
		public Transform menuOrigin { get; set; }
		public Node? node { private get; set; }
		public Action<GameObject, bool> setLocked { get; set; }
		public Func<GameObject, bool> getLocked { get; set; }

		public ActionMapInput actionMapInput
		{
			get { return m_SelectionInput; }
			set { m_SelectionInput = (SelectionInput)value; }
		}
		private SelectionInput m_SelectionInput;

		private Transform m_AlternateMenuOrigin; // TODO delete if not needed
		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				m_AlternateMenuOrigin = value;
			}
		}

		public event Action<Node?> selected = delegate {};

		private void Update()
		{
			if (rayOrigin == null)
				return;

			var selectionChanged = false;

			// Change activeGameObject selection to its parent transform when parent button is pressed 
			if (m_SelectionInput.parent.wasJustPressed)
			{
				var go = Selection.activeGameObject;
				if (go != null && go.transform.parent != null)
				{
					s_SelectedObjects.Remove(go);
					s_SelectedObjects.Add(go.transform.parent.gameObject);
					selectionChanged = true;
				}
			}
			var newHoverGameObject = getFirstGameObject(rayOrigin);
			var newPrefabRoot = newHoverGameObject;

			if (newHoverGameObject != null)
			{
				// If gameObject is within a prefab and not the current prefab, choose prefab root
				newPrefabRoot = PrefabUtility.FindPrefabRoot(newHoverGameObject);
				if (newPrefabRoot != s_CurrentPrefabOpened)
					newHoverGameObject = newPrefabRoot;
			}

			//LockTEST
			if (getLocked(newHoverGameObject))
			{
				//check hover time
				return;
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

			// Handle select button press
			if (m_SelectionInput.select.wasJustPressed) 
			{
				// Detect double click
				var timeSinceLastSelect = (float)(DateTime.Now - m_LastSelectTime).TotalSeconds;
				m_LastSelectTime = DateTime.Now;
				if (U.UI.IsDoubleClick(timeSinceLastSelect))
				{
					s_CurrentPrefabOpened = m_HoverGameObject;
					s_SelectedObjects.Remove(s_CurrentPrefabOpened);
				}
				else
				{
					// Reset current prefab if selecting outside of it
					if (newPrefabRoot != s_CurrentPrefabOpened)
						s_CurrentPrefabOpened = null;

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
						s_SelectedObjects.Clear();
						s_SelectedObjects.Add(m_HoverGameObject);
					}
				}

				selectionChanged = true;
			}

			if (selectionChanged)
			{
				Selection.objects = s_SelectedObjects.ToArray();
				selected(node);
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
