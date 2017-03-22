#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Get access to locking features
	/// </summary>
	public interface IUsesGameObjectLocking
	{
	}

	public static class IUsesGameObjectLockingMethods
	{
		internal static Action<GameObject, bool> setLocked { get; set; }
		internal static Func<GameObject, bool> isLocked { get; set; }

		/// <summary>
		/// Set a GameObject's locked status
		/// </summary>
		public static void SetLocked(this IUsesGameObjectLocking obj, GameObject go, bool locked)
		{
			if (setLocked != null)
				setLocked(go, locked);
		}

		/// <summary>
		/// Check whether a GameObject is locked
		/// </summary>
		public static bool IsLocked(this IUsesGameObjectLocking obj, GameObject go)
		{
			if (isLocked != null)
				return isLocked(go);

			return false;
		}
	}
}
#endif
