#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
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
	}

	public static class IMainMenuMethods
	{
		internal static Func<Transform, Type, bool> isToolActive { get; set; }

		/// <summary>
		/// Returns true if the active tool on the given ray origin is of the given type
		/// </summary>
		/// <param name="rayOrigin">The ray origin to check</param>
		/// <param name="type">The tool type to compare</param>
		public static bool IsToolActive(this IMainMenu obj, Transform rayOrigin, Type type)
		{
			if (isToolActive != null)
				return isToolActive(rayOrigin, type);

			return false;
		}
	}
}
#endif
