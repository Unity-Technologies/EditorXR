using UnityEngine;
using System.Collections;
using UnityEngine.VR;
using UnityEngine.VR.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;
using UnityEngine.VR.Utilities;

public class SelectionTool : MonoBehaviour, ITool, IRay, IRaycaster, ICustomActionMap, IHighlight
{
	private static HashSet<GameObject> s_SelectedObjects = new HashSet<GameObject>(); // Selection set is static because multiple selection tools can simulataneously add and remove objects from a shared selection

	private GameObject m_HoverGameObject;
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

	public Node node { private get; set; }

	void Update()
	{
		if (rayOrigin == null)
			return;

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
			if (U.UI.DoubleClick(timeSinceLastSelect))
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