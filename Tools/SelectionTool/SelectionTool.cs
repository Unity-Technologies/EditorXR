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

	private static HashSet<GameObject> s_SelectedObjects = new HashSet<GameObject>(); // Selection set is static because multiple selection tools can simulataneously add and remove objects from a shared selection

	private GameObject m_HoverGameObject;
	private SelectionHelper m_SelectionHelper;
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

	public SelectionRayCast getFirstGameObject { private get; set; }

	public Transform rayOrigin { private get; set; }

	public Action<GameObject, bool> setHighlight { private get; set; }

	private Transform m_DirectTransformOldParent;

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

		//if (newHoverGameObject != null && m_SelectionHelper == null)
		//{
		//	m_SelectionHelper = newHoverGameObject.GetComponent<SelectionHelper>();
		//	// If gameObject has a SelectionHelper, check selection mode, then check selectionTarget
		//	if (m_SelectionHelper)
		//	{
		//		switch (m_SelectionHelper.selectionMode)
		//		{
		//			case SelectionHelper.SelectionMode.DIRECT:
		//				if (distance >= directRayLength)
		//				{
		//					m_SelectionHelper = null;
		//					newHoverGameObject = null;
		//				}
		//				break;
		//			case SelectionHelper.SelectionMode.REMOTE:
		//				if (distance < directRayLength)
		//				{
		//					m_SelectionHelper = null;
		//					newHoverGameObject = null;
		//				}
		//				break;
		//		}
		//	}
		//}
		if(newHoverGameObject != null) {
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
			if (m_SelectionHelper)
			{
				if (m_SelectionHelper.transformMode == SelectionHelper.TransformMode.DIRECT)
				{
					m_DirectTransformOldParent = m_SelectionHelper.selectionTarget.transform.parent;
					m_SelectionHelper.selectionTarget.transform.parent = rayOrigin;
				}
				if (m_SelectionHelper.transformMode == SelectionHelper.TransformMode.CUSTOM)
				{
					if (m_SelectionHelper.onSelect != null)
						m_SelectionHelper.onSelect.Invoke(m_SelectionHelper.transform, rayOrigin);
				}
			}
			else
			{
				// Detect double click
				var timeSinceLastSelect = (float)(DateTime.Now - m_LastSelectTime).TotalSeconds;
				m_LastSelectTime = DateTime.Now;
				if (timeSinceLastSelect < kDoubleClickIntervalMax && timeSinceLastSelect > kDoubleClickIntervalMin)
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
		if (m_SelectionInput.select.wasJustReleased)
		{
			if (m_SelectionHelper && m_SelectionHelper.transformMode == SelectionHelper.TransformMode.CUSTOM)
			{
				if (m_SelectionHelper.onRelease != null)
					m_SelectionHelper.onRelease.Invoke(m_SelectionHelper.transform, rayOrigin);
			}
			if (m_SelectionHelper && m_SelectionHelper.transformMode == SelectionHelper.TransformMode.DIRECT)
				m_SelectionHelper.selectionTarget.transform.parent = m_DirectTransformOldParent;
		}
		if (m_SelectionInput.select.isHeld)
		{
			if (m_SelectionHelper && m_SelectionHelper.transformMode == SelectionHelper.TransformMode.CUSTOM)
			{
				if (m_SelectionHelper.onHeld != null)
					m_SelectionHelper.onHeld.Invoke(m_SelectionHelper.transform, rayOrigin);
			}
		}
		else
			m_SelectionHelper = null;
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