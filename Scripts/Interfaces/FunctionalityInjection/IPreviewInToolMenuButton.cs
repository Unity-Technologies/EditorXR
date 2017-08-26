#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class the ability to preview tools in a ToolButton
	/// </summary>
	public interface IPreviewInToolMenuButton
	{
	}

	public static class IPreviewInToolMenuButtonMethods
	{
		public static Action<Transform, Type, string> previewInToolMenuButton { get; set; }

		/// <summary>
		/// Highlights a ToolMenuButton when a menu button is highlighted
		/// <param name="rayOrigin">Transform: Ray origin to check</param>
		/// <param name="toolType">Type: MenuButton's tool type to preview</param>
		/// <param name="toolDescription">String: The tool description to display as a Tooltip</param>
		/// </summary>
		public static void PreviewInToolMenuButton (this IMainMenu obj, Transform rayOrigin, Type toolType, string toolDescription)
		{
			previewInToolMenuButton(rayOrigin, toolType, toolDescription);
		}

		public static Action clearToolMenuButtonPreview { get; set; }

		/// <summary>
		/// Clears any ToolMenuButton previews that are set
		/// </summary>
		public static void ClearToolMenuButtonPreview (this IMainMenu obj)
		{
			clearToolMenuButtonPreview();
		}
	}
}
#endif
