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

		/// <summary>
		/// Collection of action map input's to disable, when this menu is enabled
		/// Alternate menu disables main menu input when the alternate menu is displayed
		/// The Main Menu is currently consuming the x axis input the alternate menu requires
		/// </summary>
		List<ActionMapInput> actionMapInputsToDisable { get; set; }

		// HACK: Awake/Start get called together in ExecuteInEditMode, so calling this method after is a workaround for order of operations
		Action setup { get; }

		/// <summary>
		/// Function called to hide the menu
		/// </summary>
		EventHandler hideAlternateMenu { get; set; }

		/// <summary>
		/// Event used to inform another object that this menu is being shown
		/// </summary>
		Action onShow { get; set; }

		/// <summary>
		/// Event used to inform another object that this menu is being hidden
		/// </summary>
		event EventHandler onHide;

		Action hide { get; }

		Action show { get; }
	}
}