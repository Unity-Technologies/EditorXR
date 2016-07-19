using UnityEngine;
using System.Collections;
using UnityEngine.VR.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.EventSystems;

public class SelectionTool : MonoBehaviour, ITool, IRay, IRaycaster, IStandardActionMap, IHighlight
{

	private float m_DoubleClickIntervalMax = 0.3f;
	private float m_DoubleClickIntervalMin = 0.15f;

	private GameObject m_CurrentOver;
	private DateTime m_LastSelectTime;

	private static GameObject s_CurrentPrefabRoot;

	public Func<Transform, GameObject> getFirstGameObject { private get; set; }

	public Transform RayOrigin { get; set; }

	public Standard StandardInput { get; set; }

	public Action<GameObject, bool> setHighlight { private get; set; }


	void Update()
	{
		var newOver = getFirstGameObject(RayOrigin);

		if (newOver != null)// If gameObject is within a prefab and not the current prefab, choose prefab root
		{
			var newPrefabRoot = PrefabUtility.FindPrefabRoot(newOver);
			if (newPrefabRoot != s_CurrentPrefabRoot)
				newOver = newPrefabRoot;
		}

		if (newOver != m_CurrentOver) // Handle changing highlight
		{
			if(m_CurrentOver != null)
				setHighlight(m_CurrentOver, false);

			if(newOver != null)
				setHighlight(newOver, true);
		}

		m_CurrentOver = newOver;

		if (StandardInput.action.wasJustPressed) // Handle select button press
		{
			// Detect double click
			var timeSinceLastSelect = (float)(DateTime.Now - m_LastSelectTime).TotalSeconds;
			m_LastSelectTime = DateTime.Now;
			if (timeSinceLastSelect < m_DoubleClickIntervalMax && timeSinceLastSelect > m_DoubleClickIntervalMin)
				s_CurrentPrefabRoot = m_CurrentOver;

			// Reset current prefab if selecting outside of it
			if (PrefabUtility.FindPrefabRoot(m_CurrentOver) != s_CurrentPrefabRoot)
				s_CurrentPrefabRoot = null;

			Selection.activeGameObject = m_CurrentOver;
		}
	}

	void OnDestroy()
	{
		if(m_CurrentOver != null)
			setHighlight(m_CurrentOver, false);
	}
}
