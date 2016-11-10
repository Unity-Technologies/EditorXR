using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Actions;
using System.Linq;
using UnityEditor;

public class LockModule : MonoBehaviour, IToolActions, ISelectionChanged
{
	class LockModuleAction : IAction
	{
		internal Func<bool> execute;
		public Sprite icon { get; internal set; }
		public void ExecuteAction()
		{
			execute();
		}
	}

	[SerializeField]
	Sprite m_LockIcon;
	[SerializeField]
	Sprite m_UnlockIcon;

	readonly LockModuleAction m_LockModuleAction = new LockModuleAction();
	public List<IAction> toolActions { get; private set; }

	// TODO: This should go away once the alternate menu stays open or if locking/unlocking from alternate menu goes 
	// away entirely (e.g. because of HierarchyWorkspace)
	public Action<Transform, GameObject> updateAlternateMenu { private get; set; }

	readonly List<GameObject> m_LockedGameObjects = new List<GameObject>();

	Dictionary<Transform, GameObject> m_CurrentHoverObjects = new Dictionary<Transform, GameObject>();
	Dictionary<Transform, float> m_HoverTimes = new Dictionary<Transform, float>();
	const float kMaxHoverTime = 2.0f;

	GameObject m_SelectedObject;
	
	void Awake()
	{
		m_LockModuleAction.execute = ToggleLocked;
		UpdateActionIcon(null);

		toolActions = new List<IAction>() { m_LockModuleAction };
	}

	public bool IsLocked(GameObject go)
	{
		return m_LockedGameObjects.Contains(go);
	}

	bool ToggleLocked()
	{
		var newLockState = !IsLocked(m_SelectedObject);
		SetLocked(m_SelectedObject, newLockState);
		return newLockState;
	}

	void SetCurrentSelectedObject(GameObject go)
	{
		m_SelectedObject = go;
		UpdateActionIcon(go);
	}

	public void SetLocked(GameObject go, bool locked)
	{
		if (!go)
			return;

		if (locked)
		{
			if (!m_LockedGameObjects.Contains(go))
				m_LockedGameObjects.Add(go);
		}
		else
		{
			if (m_LockedGameObjects.Contains(go))
				m_LockedGameObjects.Remove(go);
		}
	
		UpdateActionIcon(go);
	}

	void UpdateActionIcon(GameObject go)
	{
		m_LockModuleAction.icon = IsLocked(go) ? m_LockIcon : m_UnlockIcon;
	}

	public void CheckHover(GameObject go, Transform rayOrigin)
	{
		if (!m_CurrentHoverObjects.ContainsKey(rayOrigin))
			m_CurrentHoverObjects.Add(rayOrigin, null);

		if (go != m_CurrentHoverObjects[rayOrigin])
		{
			m_CurrentHoverObjects[rayOrigin] = go;
			m_HoverTimes[rayOrigin] = 0.0f;
		}
		else if (IsLocked(go))
		{
			m_HoverTimes[rayOrigin] += Time.unscaledDeltaTime;
			if (m_HoverTimes[rayOrigin] >= kMaxHoverTime)
			{
				var otherNode = (from item in m_HoverTimes
								 where item.Key != rayOrigin
								 select item.Value).FirstOrDefault();

				if (otherNode < 2)
				{
					SetCurrentSelectedObject(go);
					updateAlternateMenu(rayOrigin, go);
				}
			}
		}
	}

	public void OnSelectionChanged()
	{
		var go = Selection.activeGameObject;
		SetCurrentSelectedObject(go);
	}
}
