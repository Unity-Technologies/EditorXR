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
	}

	public static class IMainMenuMethods
	{
		public static Action<Transform, Type, string> previewInPinnedToolButton { get; set; }

		/// <summary>
		/// Highlights a pinned tool button when a menu button is highlighted
		/// <param name="rayOrigin">Transform: Ray origin to check</param>
		/// <param name="toolType">Type: MenuButton's tool type to preview</param>
		/// <param name="toolDescription">String: The tool description to display as a Tooltip</param>
		public static void PreviewInPinnedToolButton (this IMainMenu obj, Transform rayOrigin, Type toolType, string toolDescription)
		{
			previewInPinnedToolButton(rayOrigin, toolType, toolDescription);
		}

		public static Action clearPinnedToolButtonPreview { get; set; }

		/// <summary>
		/// Clears any PinnedToolButton previews that are set
		/// </summary>
		public static void ClearPinnedToolButtonPreview (this IMainMenu obj)
		{
			clearPinnedToolButtonPreview();
		}
	}
}
#endif
