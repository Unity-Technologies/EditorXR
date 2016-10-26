using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Actions;

public class LockModule : MonoBehaviour, IToolActions
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

	private readonly List<GameObject> m_LockedGameObjects = new List<GameObject>();
	private GameObject m_CurrentHoverObject;
	private const float kMaxHoverTime = 2.0f;
	private float m_HoverTime;


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
		SetLocked(m_CurrentHoverObject, true);
	}

	public void SetUnLocked()
	{
		SetLocked(m_CurrentHoverObject, false);
	}

	public void SetCurrentHoverObject(GameObject go)
	{
		m_CurrentHoverObject = go;
	}

	public void SetLocked(GameObject go,bool locked)
	{
		if(go == null)
			return;

		if(locked)
		{
			if(!m_LockedGameObjects.Contains(go))
			{
				m_LockedGameObjects.Add(go);
				Debug.Log(go.name + " locked");
			}
		}
		else
		{
			if(m_LockedGameObjects.Contains(go))
			{
				m_LockedGameObjects.Remove(go);
				Debug.Log(go.name + " unlocked");
			}
		}
	}

	public void CheckhoverTime(GameObject go)
	{
		if (go != m_CurrentHoverObject)
		{
			m_CurrentHoverObject = go;
			m_HoverTime = 0.0f;
			return;
		}
		else
		{
			m_HoverTime += Time.unscaledDeltaTime;
			if (m_HoverTime >= kMaxHoverTime)
			{
				//pop up radial menu
			}
		}
	}
}
