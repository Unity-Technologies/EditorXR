using UnityEngine;
using System.Collections;
using UnityEngine.VR.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;

public class SelectionTool : MonoBehaviour, ITool, IRay, IRaycaster, ICustomActionMap, IHighlight
{
	private const float kDoubleClickIntervalMax = 0.3f;
	private const float kDoubleClickIntervalMin = 0.15f;

	private static HashSet<GameObject> s_SelectedObjects = new HashSet<GameObject>();

	private GameObject m_CurrentOver;
	private DateTime m_LastSelectTime;

	private static GameObject s_CurrentPrefabRoot;

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

	public Transform rayOrigin { get; set; }

	public Action<GameObject, bool> setHighlight { private get; set; }

	void Update()
	{
		if (rayOrigin == null)
			return;

		// Handle parent button press
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

		var newOver = getFirstGameObject(rayOrigin);

		if (newOver != null)
		{
			// If gameObject is within a prefab and not the current prefab, choose prefab root
			var newPrefabRoot = PrefabUtility.FindPrefabRoot(newOver);
			if (newPrefabRoot != s_CurrentPrefabRoot)
				newOver = newPrefabRoot;
		}

		// Handle changing highlight
		if (newOver != m_CurrentOver)
		{
			if(m_CurrentOver != null)
				setHighlight(m_CurrentOver, false);

			if(newOver != null)
				setHighlight(newOver, true);
		}

		m_CurrentOver = newOver;

		// Handle select button press
		if (m_SelectionInput.select.wasJustPressed) 
		{
			// Detect double click
			var timeSinceLastSelect = (float)(DateTime.Now - m_LastSelectTime).TotalSeconds;
			m_LastSelectTime = DateTime.Now;
			if (timeSinceLastSelect < kDoubleClickIntervalMax && timeSinceLastSelect > kDoubleClickIntervalMin)
			{
				s_CurrentPrefabRoot = m_CurrentOver;
				s_SelectedObjects.Remove(s_CurrentPrefabRoot);
			}
			else
			{
				// Reset current prefab if selecting outside of it
				if (PrefabUtility.FindPrefabRoot(m_CurrentOver) != s_CurrentPrefabRoot)
					s_CurrentPrefabRoot = null;

				// Multi-Select
				if (m_SelectionInput.multiselect.isHeld)
				{
					if (s_SelectedObjects.Contains(m_CurrentOver)) // Remove from selection
					{
						s_SelectedObjects.Remove(m_CurrentOver);
					}
					else
					{
						s_SelectedObjects.Add(m_CurrentOver); // Add to selection
						Selection.activeGameObject = m_CurrentOver;
					}
				}
				else
				{
					s_SelectedObjects.Clear();
					Selection.activeGameObject = m_CurrentOver;
					s_SelectedObjects.Add(m_CurrentOver);
				}
			}
			Selection.objects = s_SelectedObjects.ToArray();
		}
	}

	void OnDisable()
	{
		if (m_CurrentOver != null)
		{
			setHighlight(m_CurrentOver, false);
			m_CurrentOver = null;
		}
	}
}
