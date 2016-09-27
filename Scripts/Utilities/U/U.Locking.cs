namespace UnityEngine.VR.Utilities
{
	using UnityEngine;
	using System.Collections.Generic;
#if UNITY_EDITOR
	using UnityEditor.VR;
	using UnityEditor;
#endif

	public static partial class U
	{
		/// <summary>
		/// Locking/Unlocking EditorVR utilities
		/// </summary>
		public static class Locking
		{
			public static List<GameObject> s_LockedGameObjects = new List<GameObject>();
			private static GameObject s_CurrentHoverObject = null;
			private static float m_TimeStamp = 0.0f;

			public static void LockGameObject(GameObject go)
			{
				if(!s_LockedGameObjects.Contains(go))
				{
					s_LockedGameObjects.Add(go);
					Selection.activeGameObject = null;
					Debug.Log(go.name + " locked");
				}
			}

			public static void LockCurrentSelectedGameObject()
			{
				LockGameObject(s_CurrentHoverObject);
			}

			public static void UnlockCurrentSelectedGameObject()
			{
				if(IsCurrentHoverObjectReadyToUnlock())
				{
					UnLockGameObject(s_CurrentHoverObject);
				}
			}

			public static void UnLockGameObject(GameObject go)
			{
				if(s_LockedGameObjects.Contains(go))
				{
					s_LockedGameObjects.Remove(go);
					Debug.Log(go.name + " unlocked");
				}
			}

			public static void UnLockAllGameObjects()
			{
				s_LockedGameObjects.Clear();
            }

			public static bool IsGameObjectLocked(GameObject go)
			{
				return s_LockedGameObjects.Contains(go);
			}

			public static void SetCurrentHoverObject(GameObject go)
			{
				if(go == null)
					return;

				if(s_CurrentHoverObject != go)
				{
					if(s_CurrentHoverObject != null && s_CurrentHoverObject.GetComponent<LockItem>())
						U.Object.Destroy(s_CurrentHoverObject.GetComponent<LockItem>());

					s_CurrentHoverObject = go;
					m_TimeStamp = Time.realtimeSinceStartup;

					if(!s_CurrentHoverObject.GetComponent<LockItem>())
						s_CurrentHoverObject.AddComponent<LockItem>();
				}
			}

			public static bool IsCurrentHoverObjectReadyToUnlock()
			{
				if(s_CurrentHoverObject == null)
					return false;

				if(Time.realtimeSinceStartup - m_TimeStamp > 2.0f)
					return true;
				else
					return false;
			}
		}
	}
}