using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VR;

public class LockModule : MonoBehaviour
{
	private readonly List<GameObject> m_LockedGameObjects = new List<GameObject>();
	private GameObject m_CurrentHoverObject;
	private const float kMaxHoverTime = 2.0f;
	private float m_HoverTime;

	void Awake()
	{
		m_CurrentHoverObject = null;
		m_HoverTime = 0.0f;
		GameObject[] temp = GameObject.FindGameObjectsWithTag("testcube");
		foreach(GameObject go in temp)
		{
			SetLocked(go, true);
		}
	}

	public bool GetLocked(GameObject go)
	{
		return m_LockedGameObjects.Contains(go);
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
