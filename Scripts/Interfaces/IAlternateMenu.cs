using System;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Menus
{
	/// <summary>
	/// An alternate menu that shows on device proxies
	/// </summary>
	public interface IAlternateMenu : IUsesMenuActions, IUsesRayOrigin
	{
		/// <summary>
		/// Controls whether the menu is visible or not
		/// </summary>
		bool visible { get; set; }

		/// <summary>
		/// Delegate called when any item was selected in the alternate menu
		/// </summary>
		event Action<Transform> itemWasSelected;
	}
}