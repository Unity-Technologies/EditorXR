using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Get access to locking features
	/// </summary>
	public interface IUsesGameObjectLocking
	{
		/// <summary>
		/// Set a GameObject's locked status
		/// </summary>
		Action<GameObject, bool> setLocked { set; }

		/// <summary>
		/// Check whether a GameObject is locked
		/// </summary>
		Func<GameObject,bool> isLocked { set; }
	}
}