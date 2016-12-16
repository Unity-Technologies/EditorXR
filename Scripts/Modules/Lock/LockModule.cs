using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEditor;

public class LockModule : MonoBehaviour, IActions, ISelectionChanged
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
	public List<IAction> actions { get; private set; }

	// TODO: This should go away once the alternate menu stays open or if locking/unlocking from alternate menu goes 
	// away entirely (e.g. because of HierarchyWorkspace)
	public Action<Transform, GameObject> updateAlternateMenu { private get; set; }

	readonly List<GameObject> m_LockedGameObjects = new List<GameObject>();

	GameObject m_CurrentHoverObject;
	Transform m_HoverRayOrigin;
	float m_HoverDuration;
	const float kMaxHoverTime = 2.0f;
	
	void Awake()
	{
		m_LockModuleAction.execute = ToggleLocked;
		UpdateActionIcon(null);

		actions = new List<IAction>() { m_LockModuleAction };
	}

	public bool IsLocked(GameObject go)
	{
		return m_LockedGameObjects.Contains(go);
	}

	bool ToggleLocked()
	{
		var go = Selection.activeGameObject ?? m_CurrentHoverObject;
		var newLockState = !IsLocked(go);
		SetLocked(go, newLockState);
		return newLockState;
	}

	public void SetLocked(GameObject go, bool locked)
	{
		if (!go)
			return;

		if (locked)
		{
			if (!m_LockedGameObjects.Contains(go))
				m_LockedGameObjects.Add(go);

			// You shouldn't be able to keep an object selected if you are locking it
			Selection.objects = Selection.objects.Where(o => o != go).ToArray();
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

	public void OnHovered(GameObject go, Transform rayOrigin)
	{
		// Latch a new ray origin only when nothing is being hovered over
		if (Selection.activeGameObject || !m_HoverRayOrigin)
		{
			m_HoverRayOrigin = rayOrigin;
			m_CurrentHoverObject = go;
			m_HoverDuration = 0f;
		}
		else if (m_HoverRayOrigin == rayOrigin)
		{
			if (!go) // Ray origin is no longer hovering over any object
			{
				// Turn off menu if it was previously shown
				if (IsLocked(m_CurrentHoverObject))
					updateAlternateMenu(rayOrigin, null);

				m_HoverRayOrigin = null;
				m_CurrentHoverObject = null;
			}
			else if (m_CurrentHoverObject == go) // Keep track of existing hover object
			{
				m_HoverDuration += Time.unscaledDeltaTime;

				// Don't allow hover menu if over a selected game object
				if (IsLocked(go) && m_HoverDuration >= kMaxHoverTime)
				{
					UpdateActionIcon(go);

					// Open up the menu, so that locking can be changed
					updateAlternateMenu(rayOrigin, go);
				}
			}
			else // Switch to new hover object on the same ray origin
			{
				// Turn off menu if it was previously shown
				if (IsLocked(m_CurrentHoverObject))
					updateAlternateMenu(rayOrigin, null);

				m_CurrentHoverObject = go;
				m_HoverDuration = 0f;
			}
		}
	}

	public void OnSelectionChanged()
	{
		UpdateActionIcon(Selection.activeGameObject);
	}
}
