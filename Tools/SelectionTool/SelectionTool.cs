using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputNew;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Tools
{
	using RadialMenu = UnityEngine.VR.Menus.RadialMenu;

	[UnityEngine.VR.Tools.MainMenuItem("Selection", "Transform", "Select items in the scene")]
	public class SelectionTool : MonoBehaviour, ITool, IRay, IRaycaster, ICustomActionMap, IHighlight, IUsesActions, IMenuOrigins, IInstantiateUI
	{
		private static HashSet<GameObject> s_SelectedObjects = new HashSet<GameObject>(); // Selection set is static because multiple selection tools can simulataneously add and remove objects from a shared selection

		private GameObject m_HoverGameObject;
		private DateTime m_LastSelectTime;

		// The prefab (if any) that was double clicked, whose individual pieces can be selected
		private static GameObject s_CurrentPrefabOpened; 

		public ActionMap actionMap { get { return m_ActionMap; } }
		[SerializeField]
		private ActionMap m_ActionMap;

		[SerializeField]
		private RadialMenu m_RadialMenuPrefab;

		private RadialMenu m_RadialMenu;

		public ActionMapInput actionMapInput
		{
			get { return m_SelectionInput; }
			set { m_SelectionInput = (SelectionInput)value; }
		}
		private SelectionInput m_SelectionInput;

		public Func<Transform, GameObject> getFirstGameObject { private get; set; }
		public Transform rayOrigin { private get; set; }

		public Action<GameObject, bool> setHighlight { private get; set; }

		public List<IAction> actions { set { m_RadialMenu.allActions = value; } }

		public Transform menuOrigin { get; set; }

		private Transform m_AlternateMenuOrigin; // TODO delete if not needed
		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				m_AlternateMenuOrigin = value;

				if (m_RadialMenu != null)
					m_RadialMenu.alternateMenuOrigin = value;
			}
		}

		public Transform menuInputOrigin { get; set; }

		public Func<IAction, bool> performAction
		{
			set
			{
				if (m_RadialMenu != null)
					m_RadialMenu.performAction = value;
				else
					Debug.LogError("Cannot set PerformAction in Radial Menu");
			}
		}

		public Func<GameObject, GameObject> instantiateUI
		{
			set
			{
				m_RadialMenu = value(m_RadialMenuPrefab.gameObject).GetComponent<RadialMenu>();
				m_RadialMenu.instantiateUI = value;
				m_RadialMenu.alternateMenuOrigin = m_AlternateMenuOrigin;
				m_RadialMenu.Setup();
			}
		}

		private void Update()
		{
			if (rayOrigin == null)
				return;

			//  TODO: Add rotational thumbstick-based selection of radial menu items
			//if (m_SelectionInput.navigateRadialMenu.isEnabled)
				//Debug.LogError("<color=yellow>Navigate Radial Menu ENABLED here</color>");

			//if (m_SelectionInput.navigateRadialMenu.rawValue != 0)
				//Debug.LogError("<color=yellow>Navigate Radial Menu Raw Value here : </color>" + m_SelectionInput.navigateRadialMenu.rawValue);

			// Change activeGameObject selection to its parent transform when parent button is pressed 
			if (m_SelectionInput.parent.wasJustPressed)
			{
				var go = Selection.activeGameObject;
				if (go != null && go.transform.parent != null)
				{
					s_SelectedObjects.Remove(go);
					s_SelectedObjects.Add(go.transform.parent.gameObject);
					Selection.objects = s_SelectedObjects.ToArray();
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
				if (U.Input.DoubleClick(timeSinceLastSelect))
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
						Selection.activeGameObject = m_HoverGameObject;
						s_SelectedObjects.Add(m_HoverGameObject);
						m_RadialMenu.Show(); // Show the radial menu if there are any objects in the set, hide it otherwise.
					}
				}
				Selection.objects = s_SelectedObjects.ToArray();
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
