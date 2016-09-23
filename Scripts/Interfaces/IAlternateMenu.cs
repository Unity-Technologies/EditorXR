using System;
using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Actions;

namespace UnityEngine.VR.Menus
{
	public interface IAlternateMenu
	{
		/// <summary>
		/// The menu actions populating the menu
		/// </summary>
		List<IAction> menuActions { set; }

		/// <summary>
		/// Delegate used peform actions from the menu
		/// </summary>
		Func<IAction, bool> performAction { set; }

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
		/// Function called to hide the menu
		/// </summary>
		EventHandler hideAlternateMenu { get; set; }

		/// <summary>
		/// Delegate called when an item is selected in the alternate menu
		/// </summary>
		Action<Node?> selected { get; set; }

		/// <summary>
		/// Event used to inform another object that this menu is being shown
		/// </summary>
		//Action<Node?> onShow { get; set; }

		/// <summary>
		/// Event used to inform another object that this menu is being hidden
		/// </summary>
		//Action<Node?> onHide { get; set; }

		//Action show { get; }
	}
}