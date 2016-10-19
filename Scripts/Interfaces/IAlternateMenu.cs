using System;
using System.Collections.Generic;
using UnityEngine.VR.Actions;

namespace UnityEngine.VR.Menus
{
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

		// HACK: Awake/Start get called together in ExecuteInEditMode, so calling this method after is a workaround for order of operations
		Action setup { get; }

		/// <summary>
		/// Delegate called when an item is selected in the alternate menu
		/// </summary>
		event Action<Node?> itemSelected;
	}
}