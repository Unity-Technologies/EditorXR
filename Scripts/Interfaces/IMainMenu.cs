using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Menus
{
	public interface IMainMenu : IMenuActions
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

		/// <summary>
		/// You must implement and call this event when the visibility of the menu changes
		/// Parameters: main menu instance
		/// </summary>
		event Action<IMainMenu> menuVisibilityChanged;

		// HACK: Awake/Start get called together in ExecuteInEditMode, so calling this method after is a workaround for order of operations
		Action setup { get; }
	}
}