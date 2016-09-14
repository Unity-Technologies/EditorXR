using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Menus
{
	public interface IMainMenu
	{
		/// <summary>
		/// The menu tools that will populate the menu
		/// </summary>
		List<Type> menuTools { set; }

		/// <summary>
		/// Delegate used select tools from the Main Menu
		/// </summary>
		Func<Node, Type, bool> selectTool { set; }

		/// <summary>
		/// The workspaces that are selectable from the menu
		/// </summary>
		List<Type> menuWorkspaces { set; }

		/// <summary>
		/// Delegate used for creating a workspace selected from the Main Menu
		/// </summary>
		Action<Type> createWorkspace { set; }

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
		/// Delegate used to inform another object that this menu is being shown
		/// </summary>
		Action menuShowing { get; set; }
	}
}