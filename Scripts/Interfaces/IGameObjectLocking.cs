using System;

namespace UnityEngine.Experimental.EditorVR.Tools
{
	/// <summary>
	/// Get access to locking features
	/// </summary>
	public interface IGameObjectLocking
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