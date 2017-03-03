#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// An alternate menu that shows on device proxies
	/// </summary>
	public interface IAlternateMenu : IMenu, IUsesMenuActions, IUsesRayOrigin
	{
		/// <summary>
		/// Delegate called when any item was selected in the alternate menu
		/// </summary>
		event Action<Transform> itemWasSelected;
	}
}
#endif
