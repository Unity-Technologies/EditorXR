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
		internal Action execute;
		public Sprite icon { get; internal set; }
		public bool ExecuteAction()
		{
			execute();
			return true;
		}
	}

	[SerializeField]
	Sprite m_LockIcon;
	[SerializeField]
	Sprite m_UnLockIcon;

	readonly LockModuleAction m_LockModuleAction = new LockModuleAction();
	readonly LockModuleAction m_UnLockModuleAction = new LockModuleAction();
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
		m_LockModuleAction.execute = SetLocked;

		m_UnLockModuleAction.icon = m_UnLockIcon;
		m_LockModuleAction.execute = SetUnLocked;

		toolActions = new List<IAction>() { m_LockModuleAction, m_UnLockModuleAction };
	}

	public bool GetLocked(GameObject go)
	{
		return m_LockedGameObjects.Contains(go);
	}

	public void SetLocked()
	{
		SetLocked(m_SelectedObject, true);
	}

	public void SetUnLocked()
	{
		SetLocked(m_SelectedObject, false);
	}

	private void SetCurrentSelectedObject(GameObject go)
	{
		m_SelectedObject = go;
	}

	public void SetLocked(GameObject go, bool locked)
	{
		if (go == null)
			return;

		if (locked)
		{
			if (!m_LockedGameObjects.Contains(go))
			{
				m_LockedGameObjects.Add(go);
				Debug.Log(go.name + " locked");
			}
		}
		else
		{
			if (m_LockedGameObjects.Contains(go))
			{
				m_LockedGameObjects.Remove(go);
				Debug.Log(go.name + " unlocked");
			}
		}
	}

	public void CheckHover(GameObject go, Node? node)
	{
		if (!m_CurrentHoverObjects.ContainsKey(node))
			m_CurrentHoverObjects.Add(node, null);

		if (go != m_CurrentHoverObjects[node])
		{
			m_CurrentHoverObjects[node] = go;
			m_HoverTimes[node] = 0.0f;
		}
		else if (GetLocked(go))
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
	}

	public void OnSelectionChanged()
	{
		SetCurrentSelectedObject(UnityEditor.Selection.activeGameObject);
	}
}
