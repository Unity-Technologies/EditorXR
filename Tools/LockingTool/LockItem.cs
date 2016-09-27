using UnityEngine;
using UnityEngine.VR.Utilities;
using System.Collections;

[ExecuteInEditMode]
public class LockItem : MonoBehaviour
{
	[SerializeField]
	private bool m_Locked;

	void Awake()
	{
		if(U.Locking.IsGameObjectLocked(gameObject))
			m_Locked = true;
		else
			m_Locked = false;
	}

	void Update()
	{
		if(m_Locked)
		{
			if(!U.Locking.IsGameObjectLocked(gameObject))
			{
				U.Locking.LockGameObject(gameObject);
			}
		}
		else
		{
			if(U.Locking.IsGameObjectLocked(gameObject))
			{
				U.Locking.UnLockGameObject(gameObject);	
			}
		}
	}
}
