using System;
using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR.Menus
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