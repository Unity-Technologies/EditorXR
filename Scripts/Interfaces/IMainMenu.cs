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
		/// Delegate used select tools from the Main Menu
		/// </summary>
		Func<Node, Type, bool> selectTool { set; }

		/// <summary>
		/// The workspaces that are selectable from the menu
		/// </summary>
		List<Type> menuWorkspaces { set; }

		/// <summary>
		/// Delegated used for creating a workspace selected from the Main Menu
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
	}
}