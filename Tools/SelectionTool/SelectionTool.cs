using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputNew;

namespace UnityEngine.Experimental.EditorVR.Tools
{
	public class SelectionTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap, ISetHighlight, IGameObjectLocking
	{
		static HashSet<GameObject> s_SelectedObjects = new HashSet<GameObject>(); // Selection set is static because multiple selection tools can simulataneously add and remove objects from a shared selection

		GameObject m_HoverGameObject;
		GameObject m_PressedObject;
		DateTime m_LastSelectTime;

		// The prefab (if any) that was double clicked, whose individual pieces can be selected
		static GameObject s_CurrentPrefabOpened;

		public ActionMap actionMap { get { return m_ActionMap; } }
		[SerializeField]
		ActionMap m_ActionMap;

		public Func<Transform, GameObject> getFirstGameObject { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action<GameObject, bool> setHighlight { private get; set; }
		public Action<GameObject, bool> setLocked { get; set; }
		public Func<GameObject, bool> isLocked { get; set; }

		public Func<Transform, bool> isRayActive;
		public event Action<GameObject, Transform> hovered;
		public event Action<Transform> selected;

		public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
		{
			if (rayOrigin == null)
				return;

			if (!isRayActive(rayOrigin))
				return;

			var selectionInput = (SelectionInput)input;

			var newHoverGameObject = getFirstGameObject(rayOrigin);
			GameObject newPrefabRoot = null;
#if UNITY_EDITOR
			if (newHoverGameObject != null)
			{
				// If gameObject is within a prefab and not the current prefab, choose prefab root
				newPrefabRoot = PrefabUtility.FindPrefabRoot(newHoverGameObject);
				if (newPrefabRoot)
				{
					if (newPrefabRoot != s_CurrentPrefabOpened)
						newHoverGameObject = newPrefabRoot;
				}

				if (newHoverGameObject.isStatic)
					return;
			}
#endif

			if (hovered != null)
				hovered(newHoverGameObject, rayOrigin);

			if (isLocked(newHoverGameObject))
				return;

			// Handle changing highlight
			if (newHoverGameObject != m_HoverGameObject)
			{
				if (m_HoverGameObject != null)
					setHighlight(m_HoverGameObject, false);

				if (newHoverGameObject != null)
					setHighlight(newHoverGameObject, true);
			}

			m_HoverGameObject = newHoverGameObject;

			if (selectionInput.select.wasJustPressed && m_HoverGameObject)
			{
				m_PressedObject = m_HoverGameObject;

				consumeControl(selectionInput.select);
			}

			// Handle select button press
			if (selectionInput.select.wasJustReleased)
			{
				if (m_PressedObject == m_HoverGameObject)
				{
					s_CurrentPrefabOpened = newPrefabRoot;

					// Multi-Select
					if (selectionInput.multiSelect.isHeld)
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

						consumeControl(selectionInput.multiSelect);
					}
					else
					{
						if (s_CurrentPrefabOpened && s_CurrentPrefabOpened != m_HoverGameObject)
							s_SelectedObjects.Remove(s_CurrentPrefabOpened);

						s_SelectedObjects.Clear();
						Selection.activeGameObject = m_HoverGameObject;
						s_SelectedObjects.Add(m_HoverGameObject);
					}

					setHighlight(m_HoverGameObject, false);

					Selection.objects = s_SelectedObjects.ToArray();
					if (selected != null)
						selected(rayOrigin);
				}

				if (m_PressedObject != null)
					consumeControl(selectionInput.select);

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
