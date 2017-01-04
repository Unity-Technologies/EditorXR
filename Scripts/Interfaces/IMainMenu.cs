using System;
using System.Collections.Generic;
using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR.Menus
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
		/// The ray origin that spawned the menu and will be used for node-specific operations (e.g. selecting a tool)
		/// </summary>
		Transform targetRayOrigin { set; }

		/// <summary>
		/// Returns true if the active tool on the given ray origin is of the given type
		/// Transform: Ray origin to check
		/// Type: Type with which to compare
		/// Returns whether the active tool is of the same type
		/// </summary>
		Func<Transform, Type, bool> isToolActive { set; }
	}
}