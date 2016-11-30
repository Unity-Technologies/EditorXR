using System;
using System.Collections.Generic;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Menus
{
	/// <summary>
	/// The main menu that can be shown on device proxies
	/// </summary>
	public interface IMainMenu : IMenu, IUsesMenuActions, ISelectTool
	{
		/// <summary>
		/// The menu tools that will populate the menu
		/// </summary>
		List<Type> menuTools { set; }

		/// <summary>
		/// The workspaces that are selectable from the menu
		/// </summary>
		List<Type> menuWorkspaces { set; }

		/// <summary>
		/// You must implement and call this event when the visibility of the menu changes
		/// IMainMenu: main menu instance
		/// </summary>
		event Action<IMainMenu> menuVisibilityChanged;

		/// <summary>
		/// The ray origin that tools will spawn on
		/// </summary>
		Transform targetRayOrigin { set; }

		/// <summary>
		/// Returns true if the active tool on the given ray origin is of the given type
		/// Transform: ray origin to check
		/// Type: Type with which to compare
		/// returns whether the active tool is of the same type
		/// </summary>
		Func<Transform, Type, bool> isToolActive { set; }
	}
}