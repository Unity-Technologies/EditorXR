#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Menus;
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
		Dictionary<Type, ISettingsMenuProvider> settingsMenuProviders { set; }

		/// <summary>
		/// The ray origin that spawned the menu and will be used for node-specific operations (e.g. selecting a tool)
		/// </summary>
		Transform targetRayOrigin { set; }

		/// <summary>
		/// Highlights a pinned tool button when a menu button is highlighted
		/// Transform: Ray origin to check
		/// Type: MenuButton's tool type to preview
		/// Returns the unoccupied pinned tool button that will display the menu-button-tool preview while the button is highlighted
		/// The returned PinnedToolButton has its preview mode disabled upon a ray exit of the menu button
		/// </summary>
		Func<Transform, Type, IPinnedToolButton> previewToolInPinnedToolButton { set; }
	}
}
#endif
