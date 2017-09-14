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
		/// The types which provide a settings menu
		/// </summary>
		Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuProvider> settingsMenuProviders { set; }

		/// <summary>
		/// The types which provide a settings menu item
		/// </summary>
		Dictionary<KeyValuePair<Type, Transform>, ISettingsMenuItemProvider> settingsMenuItemProviders { set; }

		/// <summary>
		/// The ray origin that spawned the menu and will be used for node-specific operations (e.g. selecting a tool)
		/// </summary>
		Transform targetRayOrigin { set; }

		/// <summary>
		/// Does this menu have focus?
		/// </summary>
		bool focus { get; }

		/// <summary>
		/// Get this menu's settings menu for a given type
		/// </summary>
		/// <param name="providerType">The type for which we want to get a settings menu</param>
		/// <returns></returns>
		GameObject GetSettingsMenuInstance(Type providerType);

		/// <summary>
		/// Get this menu's settings menu item for a given type
		/// </summary>
		/// <param name="providerType">The type for which we want to get a settings menu item</param>
		/// <returns></returns>
		GameObject GetSettingsMenuItemInstance(Type providerType);
	}
}
#endif
