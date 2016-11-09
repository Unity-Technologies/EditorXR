using System;
using System.Collections.Generic;
using UnityEngine.VR.Actions;

namespace UnityEngine.VR.Menus
{
	/// <summary>
	/// An alternate menu that shows on device proxies
	/// </summary>
	public interface IAlternateMenu : IMenuActions
	{
		/// <summary>
		/// The tracked node where this menu is spawned
		/// </summary>
		Node? node { set; }

		/// <summary>
		/// Controls whether the menu is visible or not
		/// </summary>
		bool visible { get; set; }

		/// <summary>
		/// Delegate called when any item was selected in the alternate menu
		/// </summary>
		event Action<Node?> itemWasSelected;
	}
}