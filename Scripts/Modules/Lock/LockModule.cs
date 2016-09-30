using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VR;

public class LockModule : MonoBehaviour
{
	private readonly List<GameObject> m_LockedGameObjects = new List<GameObject>();

	void Awake()
	{
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
}
