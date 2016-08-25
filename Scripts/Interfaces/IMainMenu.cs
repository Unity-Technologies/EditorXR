using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Menus
{
	public interface IMainMenu
	{
		/// <summary>
		/// The transform under which the menu should be parented
		/// </summary>
		Transform menuOrigin { set; }

		/// <summary>
		/// The transform under which the alternate menu should be parented
		/// </summary>
		Transform alternateMenuOrigin { set; }

		/// <summary>
		/// The menu tools that will populate the menu
		/// </summary>
		List<Type> menuTools { set; }

		/// <summary>
		/// Delegate used to test for selecting items in the Main Menu
		/// </summary>
		Func<Node, Type, bool> selectTool { set; }

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
	}
}