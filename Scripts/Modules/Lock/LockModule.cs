using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Actions;
using UnityEngine.VR.Menus;
using UnityEngine.VR;
using System.Linq;

public class LockModule : MonoBehaviour, IToolActions, ISelectionChanged
{
	class LockModuleAction : IAction
	{
		internal Func<bool> execute;
		public Sprite icon { get; internal set; }
		public bool ExecuteAction()
		{
			return execute();
		}
	}

	[SerializeField]
	Sprite m_LockIcon;
	[SerializeField]
	Sprite m_UnlockIcon;

	readonly LockModuleAction m_LockModuleAction = new LockModuleAction();
	public List<IAction> toolActions { get; private set; }

	public Action<Node?, GameObject> openRadialMenu { private get; set; }

	private readonly List<GameObject> m_LockedGameObjects = new List<GameObject>();

	private Dictionary<Node?, GameObject> m_CurrentHoverObjects = new Dictionary<Node?, GameObject>();
	private Dictionary<Node?, float> m_HoverTimes = new Dictionary<Node?, float>();
	private const float kMaxHoverTime = 2.0f;

	private GameObject m_SelectedObject;
	
	void Awake()
	{
		m_LockModuleAction.icon = m_LockIcon;
		m_LockModuleAction.execute = ToggleLocked;

		toolActions = new List<IAction>() { m_LockModuleAction };
	}

	public bool IsLocked(GameObject go)
	{
		return go && (go.isStatic || m_LockedGameObjects.Contains(go));
	}

	public bool ToggleLocked()
	{
		bool newLockState = !IsLocked(m_SelectedObject);
		SetLocked(m_SelectedObject, newLockState);
		return newLockState;
	}

	private void SetCurrentSelectedObject(GameObject go)
	{
		m_SelectedObject = go;
	}

	private void SetLocked(GameObject go, bool locked)
	{
		if (go == null)
			return;

		if (locked)
		{
			if (!m_LockedGameObjects.Contains(go))
			{
				m_LockedGameObjects.Add(go);
			}
		}
		else
		{
			if (m_LockedGameObjects.Contains(go))
			{
				m_LockedGameObjects.Remove(go);
			}
		}
		
		m_LockModuleAction.icon = locked ? m_UnlockIcon : m_LockIcon;
	}

	public void CheckHover(GameObject go, Node? node)
	{
#if true
		// We're disabling hovering over an object to bring up the radial menu for now
		return;
#else
		if (!m_CurrentHoverObjects.ContainsKey(node))
			m_CurrentHoverObjects.Add(node, null);

		if (go != m_CurrentHoverObjects[node])
		{
			m_CurrentHoverObjects[node] = go;
			m_HoverTimes[node] = 0.0f;
		}
		else if (IsLocked(go))
		{
			m_HoverTimes[node] += Time.unscaledDeltaTime;
			if (m_HoverTimes[node] >= kMaxHoverTime)
			{
				var otherNode = (from item in m_HoverTimes
								 where item.Key != node
								 select item.Value).FirstOrDefault();

				if (otherNode < 2)
					openRadialMenu(node, go);
			}
		}
#endif
	}

	public void OnSelectionChanged()
	{
		SetCurrentSelectedObject(UnityEditor.Selection.activeGameObject);
	}
}
